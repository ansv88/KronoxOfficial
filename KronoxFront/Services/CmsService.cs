using KronoxFront.DTOs;
using KronoxFront.Extensions;
using KronoxFront.ViewModels;
using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace KronoxFront.Services;

public class CmsService
{
    private readonly HttpClient _http;
    private readonly ILogger<CmsService> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly CacheService _cache;
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    public CmsService(HttpClient http, IWebHostEnvironment env, ILogger<CmsService> logger, CacheService cache)
    {
        _http = http;
        _env = env;
        _logger = logger;
        _cache = cache;
    }

    // ---------------------------------------------------
    // HERO/BANNERBILD
    // ---------------------------------------------------
    public class UpdateAltTextDto
    {
        public string AltText { get; set; } = string.Empty;
    }

    public async Task<bool> UpdatePageImageAltTextAsync(string pageKey, PageImageViewModel image)
    {
        try
        {
            var dto = new UpdateAltTextDto
            {
                AltText = image.AltText ?? string.Empty
            };

            var response = await _http.PutAsJsonAsync($"api/content/{pageKey}/images/{image.Id}/alttext", dto);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Alt-text uppdaterad för bild {ImageId} på {PageKey}", image.Id, pageKey);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to update alt text: {StatusCode} - {Content}", response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating alt text for image {ImageId} on page {PageKey}", image.Id, pageKey);
            return false;
        }
    }

    // ---------------------------------------------------
    // SIDINNEHÅLL
    // ---------------------------------------------------
    public async Task<PageContentViewModel?> GetHomeAsync()
    {
        _logger.LogInformation("Hämtar startsidans innehåll");
        var resp = await _http.GetAsync("api/content/home");
        if (!resp.IsSuccessStatusCode) return null;
        var json = await resp.Content.ReadAsStringAsync();
        var vm = json.ToViewModel();
        if (vm != null) await EnsureMemberLogosLoaded(vm);
        return vm;
    }

    public async Task<PageContentViewModel?> GetPageContentAsync(string pageKey)
    {
        return await _cache.GetPageContentAsync(pageKey, async () =>
        {
            _logger.LogInformation("Fetching page content from API: {PageKey}", pageKey);
            var resp = await _http.GetAsync($"api/content/{pageKey}");
            if (!resp.IsSuccessStatusCode) return null;
            return (await resp.Content.ReadAsStringAsync()).ToViewModel();
        });
    }

    public async Task SavePageContentAsync(string pageKey, PageContentViewModel model)
    {
        // Spara via ContentController
        var json = JsonSerializer.Serialize(model);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var resp = await _http.PutAsync($"api/content/{pageKey}", content);
        resp.EnsureSuccessStatusCode();

        // Invalidera cache efter sparning
        _cache.InvalidatePageCache(pageKey);

        // Synkronisera FeatureSections från metadata om det finns
        try
        {
            if (!string.IsNullOrEmpty(model.Metadata))
            {
                var metadata = JsonDocument.Parse(model.Metadata);

                if (metadata.RootElement.TryGetProperty("features", out var featuresElement))
                {
                    var featureSections = new List<FeatureSectionViewModel>();
                    int index = 0;

                    foreach (var feature in featuresElement.EnumerateArray())
                    {
                        featureSections.Add(new FeatureSectionViewModel
                        {
                            Title = feature.TryGetProperty("title", out var titleProp)
                                ? titleProp.GetString() ?? "" : "",
                            Content = feature.TryGetProperty("content", out var contentProp)
                                ? contentProp.GetString() ?? "" : "",
                            ImageUrl = feature.TryGetProperty("imageUrl", out var imageUrlProp)
                                ? imageUrlProp.GetString() ?? "" : "",
                            ImageAltText = feature.TryGetProperty("imageAltText", out var altTextProp)
                                ? altTextProp.GetString() ?? "" : "",
                            HasImage = feature.TryGetProperty("hasImage", out var hasImageProp)
                                ? hasImageProp.GetBoolean() : false,
                            SortOrder = index++,
                            PageKey = pageKey
                        });
                    }

                    var syncContent = new StringContent(
                        JsonSerializer.Serialize(featureSections),
                        Encoding.UTF8,
                        "application/json");

                    try
                    {
                        var syncResp = await _http.PutAsync($"api/featuresections/{pageKey}", syncContent);
                        if (!syncResp.IsSuccessStatusCode)
                        {
                            _logger.LogWarning("Kunde inte synkronisera FeatureSections. Status: {Status}",
                                syncResp.StatusCode);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Fel vid anrop till API för synkronisering av FeatureSections");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid synkronisering av FeatureSections för {PageKey}", pageKey);
        }
    }

    // ---------------------------------------------------
    // SIDBILDER
    // ---------------------------------------------------
    public async Task<PageImageViewModel?> UploadPageImageAsync(
        string pageKey, Stream fileStream, string fileName, string altText)
    {
        try
        {
            _logger.LogInformation("Laddar upp sidbild {FileName} för sida {PageKey}", fileName, pageKey);

            using var form = new MultipartFormDataContent();
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(fileName));
            form.Add(fileContent, "file", fileName);
            form.Add(new StringContent(altText ?? ""), "altText");

            var resp = await _http.PostAsync($"api/content/{pageKey}/images", form);

            if (resp.IsSuccessStatusCode)
            {
                var json = await resp.Content.ReadAsStringAsync();
                var result = json.ToImageViewModel();

                _logger.LogInformation("Sidbild {FileName} uppladdad för {PageKey} med URL {Url}", fileName, pageKey, result?.Url);

                // Invalidera cache efter bilduppladdning
                _cache.InvalidatePageCache(pageKey);
                _cache.InvalidateGroup("actionplans");

                return result;
            }
            else
            {
                var errorContent = await resp.Content.ReadAsStringAsync();
                _logger.LogError("Bilduppladning misslyckades: {StatusCode} - {Content}", resp.StatusCode, errorContent);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid uppladdning av sidbild {FileName} för {PageKey}", fileName, pageKey);
            return null;
        }
    }

    public async Task DeletePageImageAsync(string pageKey, int imageId)
    {
        _logger.LogInformation("Tar bort sidbild {ImageId} från sida {PageKey}", imageId, pageKey);
        var resp = await _http.DeleteAsync($"api/content/{pageKey}/images/{imageId}");
        resp.EnsureSuccessStatusCode();
    }

    public async Task<PageImageViewModel?> RegisterPageImageMetadataAsync(
        string pageKey, string sourcePath, string altText, bool preserveFilename = false)
    {
        _logger.LogInformation("Registrerar metadata för sidbild {SourcePath} på sida {PageKey}", sourcePath, pageKey);

        var dto = new RegisterPageImageViewModel
        {
            SourcePath = sourcePath,
            PageKey = pageKey,
            AltText = altText,
            PreserveFilename = preserveFilename
        };

        // OBS: "register" på slutet av URL:en
        var response = await _http.PostAsJsonAsync(
            $"api/content/{pageKey}/images/register",
            dto);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return json.ToImageViewModel();
    }

    // ---------------------------------------------------
    // INTRO SECTION
    // ---------------------------------------------------
    public async Task<IntroSectionViewModel> GetIntroSectionAsync(string pageKey = "home")
    {
        try
        {
            var page = await GetPageContentAsync(pageKey);
            if (page == null || string.IsNullOrEmpty(page.Metadata))
            {
                _logger.LogWarning("Kunde inte hitta intro-sektion för {PageKey}", pageKey);
                return new IntroSectionViewModel
                {
                    Title = "Välkommen",
                    Content = "<p>Innehållet är inte tillgängligt för tillfället.</p>"
                };
            }

            try
            {
                var metadata = JsonDocument.Parse(page.Metadata);
                var root = metadata.RootElement;

                if (root.TryGetProperty("introSection", out var introElement))
                {
                    var title = introElement.TryGetProperty("title", out var titleEl)
                        ? titleEl.GetString() ?? ""
                        : "";

                    var content = introElement.TryGetProperty("content", out var contentEl)
                        ? contentEl.GetString() ?? ""
                        : "";

                    var imageUrl = introElement.TryGetProperty("imageUrl", out var imageUrlEl)
                        ? imageUrlEl.GetString() ?? ""
                        : "";

                    var imageAltText = introElement.TryGetProperty("imageAltText", out var imageAltTextEl)
                        ? imageAltTextEl.GetString() ?? ""
                        : "";

                    var hasImage = introElement.TryGetProperty("hasImage", out var hasImageEl)
                        ? hasImageEl.GetBoolean()
                        : !string.IsNullOrEmpty(imageUrl);

                    var breadcrumbTitle = introElement.TryGetProperty("breadcrumbTitle", out var breadcrumbTitleEl)
                        ? breadcrumbTitleEl.GetString() ?? ""
                        : "";

                    var showNavigationButtons = introElement.TryGetProperty("showNavigationButtons", out var showNavEl)
                        ? showNavEl.GetBoolean()
                        : false;

                    var navigationButtons = new List<NavigationButtonViewModel>();
                    if (introElement.TryGetProperty("navigationButtons", out var navButtonsEl))
                    {
                        foreach (var buttonEl in navButtonsEl.EnumerateArray())
                        {
                            navigationButtons.Add(new NavigationButtonViewModel
                            {
                                Text = buttonEl.TryGetProperty("text", out var textEl) ? textEl.GetString() ?? "" : "",
                                Url = buttonEl.TryGetProperty("url", out var urlEl) ? urlEl.GetString() ?? "" : "",
                                IconClass = buttonEl.TryGetProperty("iconClass", out var iconEl) ? iconEl.GetString() ?? "fa-solid fa-arrow-right" : "fa-solid fa-arrow-right",
                                SortOrder = buttonEl.TryGetProperty("sortOrder", out var sortEl) ? sortEl.GetInt32() : 0
                            });
                        }
                    }

                    return new IntroSectionViewModel
                    {
                        Title = title,
                        Content = content,
                        ImageUrl = imageUrl,
                        ImageAltText = imageAltText,
                        HasImage = hasImage,
                        BreadcrumbTitle = breadcrumbTitle,
                        ShowNavigationButtons = showNavigationButtons,
                        NavigationButtons = navigationButtons
                    };
                }

                if (root.TryGetProperty("introTitle", out var introTitleEl) &&
                    root.TryGetProperty("introContent", out var introContentEl))
                {
                    return new IntroSectionViewModel
                    {
                        Title = introTitleEl.GetString() ?? "",
                        Content = introContentEl.GetString() ?? ""
                    };
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Kunde inte tolka metadata för {PageKey}", pageKey);
            }

            // Fallback om inga data hittades i metadata
            return new IntroSectionViewModel
            {
                Title = "Välkommen",
                Content = "<p>Innehållet är inte tillgängligt för tillfället.</p>",
                HasImage = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid hämtning av intro-sektion: {Message}", ex.Message);

            return new IntroSectionViewModel
            {
                Title = "Välkommen",
                Content = "<p>Ett fel uppstod vid hämtning av innehållet.</p>"
            };
        }
    }

    public async Task SaveIntroSectionAsync(string pageKey, IntroSectionViewModel introSection)
    {
        _logger.LogInformation("Sparar intro-sektion för sida: {PageKey}", pageKey);

        try
        {
            var pageContent = await GetPageContentAsync(pageKey) ?? new PageContentViewModel
            {
                PageKey = pageKey,
                Title = GetDefaultPageTitle(pageKey),
                HtmlContent = "",
                LastModified = DateTime.Now
            };

            var existingMetadata = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(pageContent.Metadata))
            {
                try
                {
                    var existing = JsonDocument.Parse(pageContent.Metadata);
                    foreach (var prop in existing.RootElement.EnumerateObject())
                    {
                        existingMetadata[prop.Name] = prop.Value.Clone();
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Kunde inte tolka befintlig metadata för {PageKey}", pageKey);
                }
            }

            existingMetadata["introSection"] = new
            {
                title = introSection.Title,
                content = introSection.Content,
                imageUrl = introSection.ImageUrl,
                imageAltText = introSection.ImageAltText,
                hasImage = introSection.HasImage,
                breadcrumbTitle = introSection.BreadcrumbTitle,
                showNavigationButtons = introSection.ShowNavigationButtons,
                navigationButtons = introSection.NavigationButtons.Select(nb => new
                {
                    text = nb.Text,
                    url = nb.Url,
                    iconClass = nb.IconClass,
                    sortOrder = nb.SortOrder
                }).ToArray()
            };

            pageContent.Metadata = JsonSerializer.Serialize(existingMetadata);
            pageContent.LastModified = DateTime.Now;

            var json = JsonSerializer.Serialize(pageContent);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp = await _http.PutAsync($"api/content/{pageKey}", content);
            resp.EnsureSuccessStatusCode();

            _cache.InvalidatePageCache(pageKey);

            _logger.LogInformation("Intro-sektion sparad för {PageKey}", pageKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid sparande av intro-sektion för {PageKey}", pageKey);
            throw;
        }
    }

    // ---------------------------------------------------
    // FEATURE SECTIONS
    // ---------------------------------------------------
    public async Task<List<FeatureSectionViewModel>> GetFeatureSectionsAsync(string pageKey, bool includePrivate = false)
    {
        if (!includePrivate)
        {
            return await _cache.GetOrSetAsync($"features_public_{pageKey}", async () =>
            {
                _logger.LogInformation("Fetching public feature sections from API: {PageKey}", pageKey);

                try
                {
                    var response = await _http.GetAsync($"api/featuresections/{pageKey}");
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var sectionsWithPrivate = JsonSerializer.Deserialize<List<FeatureSectionWithPrivateDto>>(json, _jsonOptions) ?? new();
                        return MapToViewModelsWithPrivate(sectionsWithPrivate, includePrivate: false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching public feature sections for {PageKey}", pageKey);
                }

                return await GetFeatureSectionsFromMetadata(pageKey);
            }, TimeSpan.FromMinutes(8), $"features_{pageKey}") ?? new List<FeatureSectionViewModel>();
        }
        else
        {
            // Inloggat innehåll
            return await _cache.GetOrSetAsync($"features_private_{pageKey}", async () =>
            {
                _logger.LogInformation("Fetching authenticated feature sections from API: {PageKey}", pageKey);
                try
                {
                    var response = await _http.GetAsync($"api/featuresections/{pageKey}/authenticated");
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var sectionsWithPrivate = JsonSerializer.Deserialize<List<FeatureSectionWithPrivateDto>>(json, _jsonOptions) ?? new();
                        return MapToViewModelsWithPrivate(sectionsWithPrivate);
                    }
                    return await GetFeatureSectionsFromMetadata(pageKey);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching authenticated feature sections for {PageKey}", pageKey);
                    return await GetFeatureSectionsFromMetadata(pageKey);
                }
            }, TimeSpan.FromMinutes(8), $"features_{pageKey}") ?? new List<FeatureSectionViewModel>();
        }
    }

    public async Task SaveFeatureSectionsAsync(string pageKey, List<FeatureSectionViewModel> sections)
    {
        try
        {
            var dtos = sections.Select(s => new FeatureSectionWithPrivateDto
            {
                Id = s.Id,
                Title = s.Title,
                Content = s.Content,
                ImageUrl = s.ImageUrl,
                ImageAltText = s.ImageAltText,
                HasImage = s.HasImage,
                SortOrder = s.SortOrder,
                HasPrivateContent = s.HasPrivateContent,
                PrivateContent = s.PrivateContent,
                ContactHeading = s.ContactHeading,
                ContactPersons = s.ContactPersons.Select(c => new ContactPersonDto
                {
                    Name = c.Name,
                    Email = c.Email,
                    Phone = c.Phone,
                    Organization = c.Organization,
                }).ToList()
            }).ToList();

            var response = await _http.PutAsJsonAsync($"api/featuresections/{pageKey}", dtos);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to save feature sections: {response.StatusCode}");
            }

            _cache.InvalidateGroup($"features_{pageKey}");
            await UpdateMetadataWithFeatureSections(pageKey, sections);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid sparande av feature-sektioner för {PageKey}", pageKey);
            throw;
        }
    }

    // ---------------------------------------------------
    // FAQ SECTIONS
    // ---------------------------------------------------  
    public async Task<List<FaqSectionViewModel>> GetFaqSectionsAsync(string pageKey)
    {
        return await _cache.GetOrSetAsync($"faq_{pageKey}", async () =>
        {
            _logger.LogInformation("Fetching FAQ sections from API: {PageKey}", pageKey);

            try
            {
                var response = await _http.GetAsync($"api/faq/{pageKey}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var faqSections = JsonSerializer.Deserialize<List<FaqSectionDto>>(json, _jsonOptions);

                    return faqSections?.Select(fs => new FaqSectionViewModel
                    {
                        Id = fs.Id,
                        PageKey = fs.PageKey,
                        Title = fs.Title,
                        Description = fs.Description,
                        SortOrder = fs.SortOrder,
                        FaqItems = fs.FaqItems.Select(fi => new FaqItemViewModel
                        {
                            Id = fi.Id,
                            FaqSectionId = fi.FaqSectionId,
                            Question = fi.Question,
                            Answer = fi.Answer,
                            ImageUrl = fi.ImageUrl,
                            ImageAltText = fi.ImageAltText,
                            HasImage = fi.HasImage,
                            SortOrder = fi.SortOrder
                        }).ToList()
                    }).ToList() ?? new List<FaqSectionViewModel>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching FAQ sections for {PageKey}", pageKey);
            }

            return new List<FaqSectionViewModel>();
        }, TimeSpan.FromMinutes(8), $"faq_{pageKey}") ?? new List<FaqSectionViewModel>();
    }

    public async Task SaveFaqSectionsAsync(string pageKey, List<FaqSectionViewModel> faqSections)
    {
        try
        {
            var faqSectionDtos = faqSections.Select(fs => new FaqSectionDto
            {
                Id = fs.Id,
                PageKey = fs.PageKey,
                Title = fs.Title,
                Description = fs.Description,
                SortOrder = fs.SortOrder,
                FaqItems = fs.FaqItems.Select(fi => new FaqItemDto
                {
                    Id = fi.Id,
                    FaqSectionId = fi.FaqSectionId,
                    Question = fi.Question,
                    Answer = fi.Answer,
                    ImageUrl = fi.ImageUrl,
                    ImageAltText = fi.ImageAltText,
                    HasImage = fi.HasImage,
                    SortOrder = fi.SortOrder
                }).ToList()
            }).ToList();

            var response = await _http.PutAsJsonAsync($"api/faq/page/{pageKey}", faqSectionDtos);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Kunde inte spara FAQ-sektioner: {StatusCode} - {Content}",
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to save FAQ sections: {response.StatusCode}");
            }

            _cache.InvalidateGroup($"faq_{pageKey}");
            _logger.LogInformation("FAQ-sektioner sparade för {PageKey}", pageKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid sparning av FAQ-sektioner för {PageKey}", pageKey);
            throw;
        }
    }

    public async Task DeleteFaqSectionsAsync(string pageKey)
    {
        try
        {
            await _http.DeleteAsync($"api/faq/page/{pageKey}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid borttagning av FAQ-sektioner för {PageKey}", pageKey);
            throw;
        }
    }

    // ---------------------------------------------------
    // HANDLINGSPLANER
    // ---------------------------------------------------
    public async Task<ActionPlanTableViewModel> GetActionPlanAsync(string pageKey)
    {
        return await _cache.GetActionPlanAsync(pageKey, async () =>
        {
            _logger.LogInformation("Fetching action plan from API: {PageKey}", pageKey);

            try
            {
                var response = await _http.GetAsync($"api/actionplan/{pageKey}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var dto = JsonSerializer.Deserialize<ActionPlanTableDto>(json, _jsonOptions);

                    return new ActionPlanTableViewModel
                    {
                        Id = dto?.Id ?? 0,
                        PageKey = pageKey,
                        LastModified = dto?.LastModified ?? DateTime.Now,
                        Items = dto?.Items?.Select(i => new ActionPlanItem
                        {
                            Id = i.Id,
                            Priority = i.Priority,
                            Module = i.Module,
                            Activity = i.Activity,
                            DetailedDescription = i.DetailedDescription,
                            PlannedDelivery = i.PlannedDelivery,
                            Completed = i.Completed,
                            SortOrder = i.SortOrder
                        }).ToList() ?? new List<ActionPlanItem>()
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching action plan for {PageKey}", pageKey);
            }

            return new ActionPlanTableViewModel { PageKey = pageKey, Items = new List<ActionPlanItem>() };
        }) ?? new ActionPlanTableViewModel { PageKey = pageKey, Items = new List<ActionPlanItem>() };
    }

    public async Task SaveActionPlanAsync(string pageKey, ActionPlanTableViewModel actionPlan)
    {
        try
        {
            var dto = new ActionPlanTableDto
            {
                Id = actionPlan.Id,
                PageKey = pageKey,
                LastModified = DateTime.Now,
                Items = actionPlan.Items.Select(i => new ActionPlanItemDto
                {
                    Id = i.Id,
                    Priority = i.Priority,
                    Module = i.Module,
                    Activity = i.Activity,
                    PlannedDelivery = i.PlannedDelivery,
                    Completed = i.Completed,
                    SortOrder = i.SortOrder
                }).ToList()
            };

            var response = await _http.PutAsJsonAsync($"api/actionplan/{pageKey}", dto);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to save action plan: {response.StatusCode}");
            }

            _cache.InvalidateGroup("actionplans");
            _logger.LogInformation("Action plan saved and cache invalidated for {PageKey}", pageKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid sparning av handlingsplan för {PageKey}", pageKey);
            throw;
        }
    }

    // ---------------------------------------------------
    // FEATURE SECTION IMAGES
    // ---------------------------------------------------
    public async Task<PageImageViewModel?> UploadFeatureImageAsync(
        string pageKey, Stream fileStream, string fileName, string sectionIndex)
    {
        _logger.LogInformation("Laddar upp feature-bild {FileName} för sektion {SectionIndex} på sida {PageKey}",
            fileName, sectionIndex, pageKey);

        try
        {
            using var form = new MultipartFormDataContent();
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(fileName));

            form.Add(fileContent, "file", fileName);
            form.Add(new StringContent(sectionIndex), "altText");
            form.Add(new StringContent("true"), "preserveFilename");

            var response = await _http.PostAsync($"api/content/{pageKey}/images", form);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var dto = json.ToImageViewModel();

                // Rensa cache för att synka om FeatureSections
                _cache.InvalidatePageCache(pageKey);
                _cache.InvalidateGroup($"features_{pageKey}");
                _cache.InvalidateKey($"features_public_{pageKey}");
                _cache.InvalidateKey($"features_private_{pageKey}");

                _logger.LogInformation("Feature-bild uppladdad: {Url}", dto?.Url);
                return dto;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Feature-bilduppladdning misslyckades: {StatusCode} - {Content}", response.StatusCode, errorContent);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid uppladdning av feature-bild {FileName} för {PageKey}", fileName, pageKey);
            return null;
        }
    }

    // ---------------------------------------------------
    // MEMBER LOGOS
    // ---------------------------------------------------
    public async Task<List<MemberLogoViewModel>> GetMemberLogosAsync()
    {
        var response = await _http.GetAsync("api/cms/logos");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return json.ToLogoViewModels() ?? new();
    }

    public async Task<MemberLogoViewModel?> RegisterLogoMetadataAsync(string sourcePath,
                                                                      string originalFileName,
                                                                      string altText,
                                                                      int sortOrd,
                                                                      string linkUrl = "")
    {
        var dto = new RegisterMemberLogoViewModel
        {
            SourcePath = sourcePath,
            OriginalFileName = originalFileName,
            AltText = altText,
            SortOrd = sortOrd,
            LinkUrl = linkUrl
        };

        var resp = await _http.PostAsJsonAsync("api/cms/logos/register", dto);
        if (!resp.IsSuccessStatusCode) return null;
        var json = await resp.Content.ReadAsStringAsync();
        return json.ToLogoViewModel();
    }

    public async Task<MemberLogoViewModel?> UploadMemberLogoAsync(MemberLogoUploadDto uploadDto)
    {
        try
        {
            using var content = new MultipartFormDataContent();

            if (uploadDto.File != null)
            {
                var stream = uploadDto.File.OpenReadStream(maxAllowedSize: 2 * 1024 * 1024);
                var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(uploadDto.File.ContentType));
                content.Add(fileContent, "File", uploadDto.File.Name);
            }

            if (!string.IsNullOrEmpty(uploadDto.AltText))
                content.Add(new StringContent(uploadDto.AltText), "AltText");

            if (!string.IsNullOrEmpty(uploadDto.LinkUrl))
                content.Add(new StringContent(uploadDto.LinkUrl), "LinkUrl");

            if (uploadDto.SortOrd > 0)
                content.Add(new StringContent(uploadDto.SortOrd.ToString()), "SortOrd");

            var response = await _http.PostAsync("api/cms/logos/upload", content);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var logoDto = JsonSerializer.Deserialize<MemberLogoDto>(json, _jsonOptions);
                return logoDto?.ToViewModel();
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Medlemslogotyp uppladdning misslyckades: {StatusCode} - {Content}", response.StatusCode, errorContent);
            throw new HttpRequestException($"Upload failed: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid uppladdning av medlemslogotyp");
            throw;
        }
    }

    public async Task UpdateMemberLogoDescriptionAsync(int logoId, string description)
    {
        try
        {
            var dto = new { Description = description };
            var response = await _http.PutAsJsonAsync($"api/cms/logos/{logoId}/description", dto);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Kunde inte uppdatera logotypbeskrivning: {StatusCode} - {Content}", response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to update description: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid uppdatering av logotypbeskrivning för logoId: {LogoId}", logoId);
            throw;
        }
    }

    public async Task UpdateMemberLogoLinkAsync(int logoId, string linkUrl)
    {
        try
        {
            var dto = new { LinkUrl = linkUrl };
            var response = await _http.PutAsJsonAsync($"api/cms/logos/{logoId}/link", dto);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Kunde inte uppdatera logotyplänk: {StatusCode} - {Content}", response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to update link: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid uppdatering av logotyplänk för logoId: {LogoId}", logoId);
            throw;
        }
    }

    public async Task MoveMemberLogoAsync(int logoId, int direction)
    {
        try
        {
            var dto = new { LogoId = logoId, Direction = direction };
            var response = await _http.PostAsJsonAsync("api/cms/logos/move", dto);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Kunde inte flytta logotyp: {StatusCode} - {Content}", response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to move logo: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid flytt av logotyp för logoId: {LogoId}", logoId);
            throw;
        }
    }

    public async Task DeleteMemberLogoAsync(int logoId)
    {
        try
        {
            var response = await _http.DeleteAsync($"api/cms/logos/{logoId}");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Kunde inte ta bort logotyp: {StatusCode} - {Content}", response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to delete logo: {response.StatusCode}");
            }

            _logger.LogInformation("Medlemslogotyp borttagen: {LogoId}", logoId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid borttagning av logotyp för logoId: {LogoId}", logoId);
            throw;
        }
    }

    // ---------------------------------------------------
    // SECTION CONFIGURATION
    // ---------------------------------------------------
    public async Task<List<SectionConfigItem>> GetPageSectionConfigAsync(string pageKey)
    {
        try
        {
            _logger.LogInformation("Hämtar sektionskonfiguration för {PageKey}", pageKey);

            var pageContent = await GetPageContentAsync(pageKey);
            if (pageContent != null && !string.IsNullOrEmpty(pageContent.Metadata))
            {
                var metadata = JsonDocument.Parse(pageContent.Metadata);
                if (metadata.RootElement.TryGetProperty("sectionConfig", out var configElement))
                {
                    var sections = new List<SectionConfigItem>();

                    foreach (var item in configElement.EnumerateArray())
                    {
                        SectionType sectionType;
                        if (item.TryGetProperty("Type", out var typeEl))
                        {
                            if (typeEl.ValueKind == JsonValueKind.String)
                            {
                                var typeString = typeEl.GetString();
                                if (!Enum.TryParse<SectionType>(typeString, out sectionType))
                                {
                                    _logger.LogWarning("Okänd sektionstyp som string: {TypeString}", typeString);
                                    continue;
                                }
                            }
                            else if (typeEl.ValueKind == JsonValueKind.Number)
                            {
                                var typeNumber = typeEl.GetInt32();
                                if (!Enum.IsDefined(typeof(SectionType), typeNumber))
                                {
                                    _logger.LogWarning("Okänd sektionstyp som nummer: {TypeNumber}", typeNumber);
                                    continue;
                                }
                                sectionType = (SectionType)typeNumber;
                            }
                            else
                            {
                                _logger.LogWarning("SectionType har oväntat format: {ValueKind}", typeEl.ValueKind);
                                continue;
                            }
                        }
                        else
                        {
                            _logger.LogWarning("SectionType saknas i konfiguration");
                            continue;
                        }

                        var isEnabled = item.TryGetProperty("IsEnabled", out var enabledEl) ? enabledEl.GetBoolean() : false;
                        var sortOrder = item.TryGetProperty("SortOrder", out var sortEl) ? sortEl.GetInt32() : 0;

                        sections.Add(new SectionConfigItem
                        {
                            Type = sectionType,
                            IsEnabled = isEnabled,
                            SortOrder = sortOrder
                        });

                        _logger.LogInformation("Laddad sektion: {Type} (enabled: {IsEnabled}, order: {SortOrder})",
                            sectionType, isEnabled, sortOrder);
                    }

                    if (sections.Any())
                    {
                        _logger.LogInformation("Sektionskonfiguration laddad: {Count} sektioner", sections.Count);
                        return sections;
                    }
                }
            }

            _logger.LogInformation("Ingen sektionskonfiguration hittades, använder fallback för {PageKey}", pageKey);
            return GetDefaultSectionConfig(pageKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid hämtning av sektionskonfiguration för {PageKey}", pageKey);
            return GetDefaultSectionConfig(pageKey);
        }
    }

    public async Task<bool> SavePageSectionConfigAsync(string pageKey, List<SectionConfigItem> sectionConfig)
    {
        try
        {
            var pageContent = await GetPageContentAsync(pageKey) ?? new PageContentViewModel
            {
                PageKey = pageKey,
                Title = GetDefaultPageTitle(pageKey),
                HtmlContent = "",
                LastModified = DateTime.Now
            };

            var existingMetadata = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(pageContent.Metadata))
            {
                try
                {
                    var existing = JsonDocument.Parse(pageContent.Metadata);
                    foreach (var prop in existing.RootElement.EnumerateObject())
                    {
                        existingMetadata[prop.Name] = JsonSerializer.Deserialize<object>(prop.Value.GetRawText());
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Kunde inte tolka befintlig metadata för {PageKey}", pageKey);
                }
            }

            existingMetadata["sectionConfig"] = sectionConfig;
            existingMetadata["lastConfigUpdate"] = DateTime.UtcNow;

            pageContent.Metadata = JsonSerializer.Serialize(existingMetadata, _jsonOptions);
            pageContent.LastModified = DateTime.Now;

            await SavePageContentAsync(pageKey, pageContent);

            _logger.LogInformation("Sektionskonfiguration sparad för {PageKey}", pageKey);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid sparning av sektionskonfiguration för {PageKey}", pageKey);
            return false;
        }
    }

    // ---------------------------------------------------
    // CONTACT INFORMATION
    // ---------------------------------------------------
    public async Task<ContactPageInfoViewModel> GetContactInfoAsync()
    {
        try
        {
            var response = await _http.GetAsync("api/contact/info");
            if (response.IsSuccessStatusCode)
            {
                var contactInfoDto = await response.Content.ReadFromJsonAsync<ContactPageInfoDto>();
                if (contactInfoDto != null)
                {
                    return new ContactPageInfoViewModel
                    {
                        PostalAddress = new ContactPostalAddressViewModel
                        {
                            OrganizationName = contactInfoDto.PostalAddress.OrganizationName,
                            AddressLine1 = contactInfoDto.PostalAddress.AddressLine1,
                            AddressLine2 = contactInfoDto.PostalAddress.AddressLine2,
                            PostalCode = contactInfoDto.PostalAddress.PostalCode,
                            City = contactInfoDto.PostalAddress.City,
                            Country = contactInfoDto.PostalAddress.Country
                        },
                        ContactPersons = contactInfoDto.ContactPersons.Select(cp => new ContactPagePersonViewModel
                        {
                            Id = cp.Id,
                            Name = cp.Name,
                            Title = cp.Title,
                            Email = cp.Email,
                            Phone = cp.Phone,
                            SortOrder = cp.SortOrder,
                            IsActive = cp.IsActive
                        }).ToList(),
                        EmailLists = contactInfoDto.EmailLists?.Select(el => new EmailListViewModel
                        {
                            Id = el.Id,
                            Name = el.Name,
                            Description = el.Description,
                            EmailAddress = el.EmailAddress,
                            SortOrder = el.SortOrder,
                            IsActive = el.IsActive
                        }).ToList() ?? new List<EmailListViewModel>()
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid hämtning av kontaktinformation");
        }

        return new ContactPageInfoViewModel();
    }

    public async Task<bool> SavePostalAddressAsync(ContactPostalAddressViewModel postalAddress)
    {
        try
        {
            var dto = new ContactPostalAddressDto
            {
                OrganizationName = postalAddress.OrganizationName,
                AddressLine1 = postalAddress.AddressLine1,
                AddressLine2 = postalAddress.AddressLine2,
                PostalCode = postalAddress.PostalCode,
                City = postalAddress.City,
                Country = postalAddress.Country
            };

            var response = await _http.PutAsJsonAsync("api/contact/postal-address", dto);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid sparning av postadress");
            return false;
        }
    }

    public async Task<List<ContactPagePersonViewModel>> GetContactPersonsAsync()
    {
        try
        {
            var response = await _http.GetAsync("api/contact/persons");
            if (response.IsSuccessStatusCode)
            {
                var contactPersonsDto = await response.Content.ReadFromJsonAsync<List<ContactPagePersonDto>>();
                if (contactPersonsDto != null)
                {
                    return contactPersonsDto.Select(cp => new ContactPagePersonViewModel
                    {
                        Id = cp.Id,
                        Name = cp.Name,
                        Title = cp.Title,
                        Email = cp.Email,
                        Phone = cp.Phone,
                        SortOrder = cp.SortOrder,
                        IsActive = cp.IsActive
                    }).ToList();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid hämtning av kontaktpersoner");
        }

        return new List<ContactPagePersonViewModel>();
    }

    public async Task<ContactPagePersonViewModel?> CreateContactPersonAsync(ContactPagePersonViewModel contactPerson)
    {
        try
        {
            var dto = new UpsertContactPagePersonDto
            {
                Name = contactPerson.Name,
                Title = contactPerson.Title,
                Email = contactPerson.Email,
                Phone = contactPerson.Phone,
                SortOrder = contactPerson.SortOrder,
                IsActive = contactPerson.IsActive
            };

            var response = await _http.PostAsJsonAsync("api/contact/persons", dto);
            if (response.IsSuccessStatusCode)
            {
                var resultDto = await response.Content.ReadFromJsonAsync<ContactPagePersonDto>();
                if (resultDto != null)
                {
                    return new ContactPagePersonViewModel
                    {
                        Id = resultDto.Id,
                        Name = resultDto.Name,
                        Title = resultDto.Title,
                        Email = resultDto.Email,
                        Phone = resultDto.Phone,
                        SortOrder = resultDto.SortOrder,
                        IsActive = resultDto.IsActive
                    };
                }
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid skapande av kontaktperson");
            return null;
        }
    }

    public async Task<bool> UpdateContactPersonAsync(ContactPagePersonViewModel contactPerson)
    {
        try
        {
            var dto = new UpsertContactPagePersonDto
            {
                Name = contactPerson.Name,
                Title = contactPerson.Title,
                Email = contactPerson.Email,
                Phone = contactPerson.Phone,
                SortOrder = contactPerson.SortOrder,
                IsActive = contactPerson.IsActive
            };

            var response = await _http.PutAsJsonAsync($"api/contact/persons/{contactPerson.Id}", dto);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid uppdatering av kontaktperson");
            return false;
        }
    }

    public async Task<bool> DeleteContactPersonAsync(int contactPersonId)
    {
        try
        {
            var response = await _http.DeleteAsync($"api/contact/persons/{contactPersonId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid borttagning av kontaktperson");
            return false;
        }
    }

    public async Task<bool> ToggleContactPersonActiveAsync(int contactPersonId)
    {
        try
        {
            var response = await _http.PutAsync($"api/contact/persons/{contactPersonId}/toggle-active", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid ändring av aktivstatus för kontaktperson");
            return false;
        }
    }

    // ============ E-POSTLISTOR ============

    public async Task<List<EmailListViewModel>> GetEmailListsAsync()
    {
        try
        {
            var response = await _http.GetAsync("api/contact/emaillists");
            if (response.IsSuccessStatusCode)
            {
                var emailListsDto = await response.Content.ReadFromJsonAsync<List<EmailListDto>>();
                if (emailListsDto != null)
                {
                    return emailListsDto.Select(el => new EmailListViewModel
                    {
                        Id = el.Id,
                        Name = el.Name,
                        Description = el.Description,
                        EmailAddress = el.EmailAddress,
                        SortOrder = el.SortOrder,
                        IsActive = el.IsActive
                    }).ToList();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid hämtning av e-postlistor");
        }

        return new List<EmailListViewModel>();
    }

    public async Task<EmailListViewModel?> CreateEmailListAsync(EmailListViewModel emailList)
    {
        try
        {
            var dto = new UpsertEmailListDto
            {
                Name = emailList.Name,
                Description = emailList.Description,
                EmailAddress = emailList.EmailAddress,
                SortOrder = emailList.SortOrder,
                IsActive = emailList.IsActive
            };

            var response = await _http.PostAsJsonAsync("api/contact/emaillists", dto);
            if (response.IsSuccessStatusCode)
            {
                var resultDto = await response.Content.ReadFromJsonAsync<EmailListDto>();
                if (resultDto != null)
                {
                    return new EmailListViewModel
                    {
                        Id = resultDto.Id,
                        Name = resultDto.Name,
                        Description = resultDto.Description,
                        EmailAddress = resultDto.EmailAddress,
                        SortOrder = resultDto.SortOrder,
                        IsActive = resultDto.IsActive
                    };
                }
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid skapande av e-postlista");
            return null;
        }
    }

    public async Task<bool> UpdateEmailListAsync(EmailListViewModel emailList)
    {
        try
        {
            var dto = new UpsertEmailListDto
            {
                Name = emailList.Name,
                Description = emailList.Description,
                EmailAddress = emailList.EmailAddress,
                SortOrder = emailList.SortOrder,
                IsActive = emailList.IsActive
            };

            var response = await _http.PutAsJsonAsync($"api/contact/emaillists/{emailList.Id}", dto);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid uppdatering av e-postlista");
            return false;
        }
    }

    public async Task<bool> DeleteEmailListAsync(int emailListId)
    {
        try
        {
            var response = await _http.DeleteAsync($"api/contact/emaillists/{emailListId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid borttagning av e-postlista");
            return false;
        }
    }

    public async Task<bool> ToggleEmailListActiveAsync(int emailListId)
    {
        try
        {
            var response = await _http.PutAsync($"api/contact/emaillists/{emailListId}/toggle-active", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid ändring av aktivstatus för e-postlista");
            return false;
        }
    }

    // ---------------------------------------------------
    // CUSTOM PAGES
    // ---------------------------------------------------
    public async Task<List<CustomPageViewModel>> GetCustomPagesAsync()
    {
        var response = await _http.GetAsync("api/custompage");
        if (response.IsSuccessStatusCode)
        {
            var dtos = await response.Content.ReadFromJsonAsync<List<CustomPageDto>>();
            return dtos?.ToViewModels() ?? new();
        }
        return new();
    }

    // ---------------------------------------------------
    // IMAGE UPLOAD
    // ---------------------------------------------------
    public async Task<string> UploadImageAsync(IBrowserFile file, string pageKey)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var stream = file.OpenReadStream(maxAllowedSize: 10_000_000);
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(file.Name));

            content.Add(fileContent, "file", file.Name);
            content.Add(new StringContent("Uppladdad bild"), "altText");

            var response = await _http.PostAsync($"api/content/{pageKey}/images", content);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var imageResult = json.ToImageViewModel();
                var url = imageResult?.Url ?? "";

                // rensa relevanta cacher så nya bilder/URLs syns direkt
                _cache.InvalidatePageCache(pageKey);
                _cache.InvalidateGroup($"features_{pageKey}");
                _cache.InvalidateKey($"features_public_{pageKey}");
                _cache.InvalidateKey($"features_private_{pageKey}");

                return url;
            }

            throw new Exception("Bilduppladdning misslyckades");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid uppladdning av bild");
            throw;
        }
    }

    // ---------------------------------------------------
    // SYNC IMAGES
    // ---------------------------------------------------
    public async Task<bool> SyncImagesFromApiAsync()
    {
        _logger.LogInformation("Synkroniserar bilder från API till wwwroot");

        var logos = await GetMemberLogosAsync();
        var membersDir = Path.Combine(_env.WebRootPath, "images", "members");
        Directory.CreateDirectory(membersDir);

        foreach (var logo in logos)
        {
            var fileName = Path.GetFileName(logo.Url);
            var localPath = Path.Combine(membersDir, fileName);
            if (!File.Exists(localPath))
            {
                var apiUrl = logo.Url.StartsWith("http")
                    ? logo.Url
                    : $"{_http.BaseAddress}{logo.Url.TrimStart('/')}";
                var rs = await _http.GetAsync(apiUrl);
                if (rs.IsSuccessStatusCode)
                {
                    await using var fs = File.Create(localPath);
                    await rs.Content.CopyToAsync(fs);
                }
            }
        }

        var page = await GetPageContentAsync("home");
        if (page != null && page.Images.Any())
        {
            foreach (var img in page.Images)
            {
                if (!img.Url.Contains("/images/pages/")) continue;

                var rel = img.Url.TrimStart('/');
                var localPath = Path.Combine(_env.WebRootPath, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);

                if (!File.Exists(localPath))
                {
                    var apiUrl = img.Url.StartsWith("http")
                        ? img.Url
                        : $"{_http.BaseAddress}{rel}";
                    var rs = await _http.GetAsync(apiUrl);
                    if (rs.IsSuccessStatusCode)
                    {
                        await using var fs = File.Create(localPath);
                        await rs.Content.CopyToAsync(fs);
                    }
                }
            }
        }

        return true;
    }

    // ---------------------------------------------------
    // PAGE KEYS (Dynamisk lista för filter/datalist)
    // ---------------------------------------------------
    public async Task<List<string>> GetAllPageKeysAsync()
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Hämta navigationens sidor (inkl. statiska system-sidor)
        try
        {
            var navResp = await _http.GetAsync("api/navigation");
            if (navResp.IsSuccessStatusCode)
            {
                var navItems = await navResp.Content.ReadFromJsonAsync<List<NavigationConfigDto>>();
                if (navItems != null)
                {
                    foreach (var n in navItems)
                    {
                        if (!string.IsNullOrWhiteSpace(n.PageKey))
                            keys.Add(n.PageKey);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Kunde inte hämta navigation för PageKeys");
        }

        // Hämta alla custom pages (kräver Admin – ok i adminvy)
        try
        {
            var custResp = await _http.GetAsync("api/custompage");
            if (custResp.IsSuccessStatusCode)
            {
                var pages = await custResp.Content.ReadFromJsonAsync<List<CustomPageDto>>();
                if (pages != null)
                {
                    foreach (var p in pages)
                    {
                        if (!string.IsNullOrWhiteSpace(p.PageKey))
                            keys.Add(p.PageKey);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Kunde inte hämta custom pages för PageKeys");
        }

        // Lägg till några vanliga standardsidor som fallback
        foreach (var k in new[] { "home", "omkonsortiet", "omsystemet", "visioner", "kontaktaoss", "dokument", "forvaltning", "forstyrelsen", "forvnsg", "medlemsnytt" })
            keys.Add(k);

        // Rensa bort icke-innehållssidor om de smugit sig med
        keys.Remove("admin");
        keys.Remove("logout");

        return keys.OrderBy(k => k).ToList();
    }

    // ---------------------------------------------------
    // PRIVATE HELPER METHODS
    // ---------------------------------------------------
    private async Task<List<FeatureSectionViewModel>> GetFeatureSectionsFromMetadata(string pageKey)
    {
        try
        {
            var page = await GetPageContentAsync(pageKey);
            if (page != null && !string.IsNullOrEmpty(page.Metadata))
            {
                var metadata = JsonDocument.Parse(page.Metadata);
                var root = metadata.RootElement;

                if (root.TryGetProperty("features", out var featuresEl))
                {
                    var features = new List<FeatureSectionViewModel>();

                    foreach (var feature in featuresEl.EnumerateArray())
                    {
                        var title = feature.TryGetProperty("title", out var titleEl)
                            ? titleEl.GetString() ?? ""
                            : "";

                        var content = feature.TryGetProperty("content", out var contentEl)
                            ? contentEl.GetString() ?? ""
                            : "";

                        var imageUrl = feature.TryGetProperty("imageUrl", out var imageUrlEl)
                            ? imageUrlEl.GetString() ?? ""
                            : "";

                        var imageAltText = feature.TryGetProperty("imageAltText", out var imageAltTextEl)
                            ? imageAltTextEl.GetString() ?? ""
                            : (string.IsNullOrEmpty(title) ? "" : title);

                        var hasImage = feature.TryGetProperty("hasImage", out var hasImageEl)
                            ? hasImageEl.GetBoolean()
                            : !string.IsNullOrEmpty(imageUrl);

                        features.Add(new FeatureSectionViewModel
                        {
                            Title = title,
                            Content = content,
                            ImageUrl = imageUrl,
                            ImageAltText = imageAltText,
                            HasImage = hasImage
                        });
                    }

                    if (features.Any())
                        return features;
                }
            }

            return new List<FeatureSectionViewModel>
            {
                new FeatureSectionViewModel
                {
                    Title = "Innehållet laddas",
                    Content = "<p>Innehållet för denna sida är under uppbyggnad.</p>",
                    HasImage = false
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid fallback-hämtning från metadata för {PageKey}", pageKey);

            return new List<FeatureSectionViewModel>
            {
                new FeatureSectionViewModel
                {
                    Title = "Ett fel uppstod",
                    Content = "<p>Det gick inte att hämta innehållet. Vänligen försök igen senare.</p>",
                    HasImage = false
                }
            };
        }
    }

    private List<FeatureSectionViewModel> MapToViewModelsWithPrivate(List<FeatureSectionWithPrivateDto> dtos, bool includePrivate = true)
    {
        return dtos.Select(dto => new FeatureSectionViewModel
        {
            Id = dto.Id,
            Title = dto.Title,
            Content = dto.Content,
            ImageUrl = dto.ImageUrl,
            ImageAltText = dto.ImageAltText,
            HasImage = dto.HasImage,
            SortOrder = dto.SortOrder,
            HasPrivateContent = dto.HasPrivateContent,
            PrivateContent = includePrivate ? dto.PrivateContent : "",
            ContactHeading = dto.ContactHeading,
            ContactPersons = dto.ContactPersons.Select(c => new ContactPersonViewModel
            {
                Name = c.Name,
                Email = c.Email,
                Phone = c.Phone,
                Organization = c.Organization,
            }).ToList()
        }).ToList();
    }

    private async Task UpdateMetadataWithFeatureSections(string pageKey, List<FeatureSectionViewModel> sections)
    {
        try
        {
            var pageContent = await GetPageContentAsync(pageKey) ?? new PageContentViewModel
            {
                PageKey = pageKey,
                Title = GetDefaultPageTitle(pageKey),
                HtmlContent = "",
                LastModified = DateTime.Now
            };

            var existingMetadata = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(pageContent.Metadata))
            {
                try
                {
                    var existing = JsonDocument.Parse(pageContent.Metadata);
                    foreach (var prop in existing.RootElement.EnumerateObject())
                    {
                        existingMetadata[prop.Name] = prop.Value.Clone();
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Kunde inte tolka befintlig metadata för {PageKey}", pageKey);
                }
            }

            existingMetadata["features"] = sections.Select(f => new
            {
                title = f.Title,
                content = f.Content,
                imageUrl = f.ImageUrl,
                imageAltText = f.ImageAltText,
                hasImage = f.HasImage,
                hasPrivateContent = f.HasPrivateContent,
                privateContent = f.PrivateContent
            }).ToArray();

            pageContent.Metadata = JsonSerializer.Serialize(existingMetadata);
            pageContent.LastModified = DateTime.Now;

            var metadataJson = JsonSerializer.Serialize(pageContent);
            var metadataContent = new StringContent(metadataJson, Encoding.UTF8, "application/json");
            var metadataResponse = await _http.PutAsync($"api/content/{pageKey}", metadataContent);
            metadataResponse.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid uppdatering av metadata för {PageKey}", pageKey);
        }
    }

    private async Task EnsureMemberLogosLoaded(PageContentViewModel pageContent)
    {
        var logos = await GetMemberLogosAsync();
        foreach (var logo in logos)
        {
            if (!logo.Url.StartsWith("/"))
                logo.Url = "/" + logo.Url;
            if (!pageContent.Images.Any(i =>
                Path.GetFileName(i.Url) == Path.GetFileName(logo.Url)))
            {
                pageContent.Images.Add(new PageImageViewModel
                {
                    Id = logo.Id,
                    Url = logo.Url,
                    AltText = logo.AltText
                });
            }
        }
    }

    private string GetDefaultPageTitle(string pageKey)
    {
        return pageKey switch
        {
            "home" => "Startsida",
            "omkonsortiet" => "Om konsortiet",
            "visioner" => "Visioner & Verksamhetsidé",
            "kontaktaoss" => "Kontakta oss",
            "omsystemet" => "Om systemet",
            "forstyrelsen" => "För styrelsen",
            "forvnsg" => "För VNSG",
            "forvaltning" => "Förvaltning",
            _ => "Sida"
        };
    }

    private List<SectionConfigItem> GetDefaultSectionConfig(string pageKey)
    {
        var defaultSections = new List<SectionConfigItem>();

        switch (pageKey.ToLower())
        {
            case "home":
                defaultSections.AddRange(new[]
                {
                    new SectionConfigItem { Type = SectionType.Banner, IsEnabled = true, SortOrder = 0 },
                    new SectionConfigItem { Type = SectionType.Intro, IsEnabled = true, SortOrder = 1 },
                    new SectionConfigItem { Type = SectionType.NavigationButtons, IsEnabled = true, SortOrder = 2 },
                    new SectionConfigItem { Type = SectionType.FeatureSections, IsEnabled = true, SortOrder = 3 },
                    new SectionConfigItem { Type = SectionType.FaqSections, IsEnabled = false, SortOrder = 4 },
                    new SectionConfigItem { Type = SectionType.MemberLogos, IsEnabled = true, SortOrder = 5 },
                    new SectionConfigItem { Type = SectionType.DocumentSection, IsEnabled = false, SortOrder = 6 }
                });
                break;

            case "dokument":
                defaultSections.AddRange(new[]
                {
                    new SectionConfigItem { Type = SectionType.Banner, IsEnabled = true, SortOrder = 0 },
                    new SectionConfigItem { Type = SectionType.Intro, IsEnabled = true, SortOrder = 1 },
                    new SectionConfigItem { Type = SectionType.NavigationButtons, IsEnabled = true, SortOrder = 2 },
                    new SectionConfigItem { Type = SectionType.FeatureSections, IsEnabled = true, SortOrder = 3 },
                    new SectionConfigItem { Type = SectionType.DocumentSection, IsEnabled = true, SortOrder = 4 }
                });
                break;

            case "kontaktaoss":
                defaultSections.AddRange(new[]
                {
                    new SectionConfigItem { Type = SectionType.Banner, IsEnabled = true, SortOrder = 0 },
                    new SectionConfigItem { Type = SectionType.Intro, IsEnabled = true, SortOrder = 1 },
                    new SectionConfigItem { Type = SectionType.NavigationButtons, IsEnabled = false, SortOrder = 2 },
                    new SectionConfigItem { Type = SectionType.FeatureSections, IsEnabled = false, SortOrder = 3 },
                    new SectionConfigItem { Type = SectionType.FaqSections, IsEnabled = false, SortOrder = 4 },
                    new SectionConfigItem { Type = SectionType.ContactForm, IsEnabled = true, SortOrder = 5 },
                    new SectionConfigItem { Type = SectionType.MemberLogos, IsEnabled = true, SortOrder = 6 }
                });
                break;

            case "forvaltning":
                defaultSections.AddRange(new[]
                {
                    new SectionConfigItem { Type = SectionType.Banner, IsEnabled = true, SortOrder = 0 },
                    new SectionConfigItem { Type = SectionType.Intro, IsEnabled = true, SortOrder = 1 },
                    new SectionConfigItem { Type = SectionType.NavigationButtons, IsEnabled = false, SortOrder = 2 },
                    new SectionConfigItem { Type = SectionType.ActionPlanTable, IsEnabled = true, SortOrder = 3 },
                    new SectionConfigItem { Type = SectionType.DevelopmentSuggestionForm, IsEnabled = true, SortOrder = 4 },
                    new SectionConfigItem { Type = SectionType.FeatureSections, IsEnabled = false, SortOrder = 5 },
                    new SectionConfigItem { Type = SectionType.FaqSections, IsEnabled = false, SortOrder = 6 },
                    new SectionConfigItem { Type = SectionType.MemberLogos, IsEnabled = true, SortOrder = 7 }
                });
                break;

            default:
                defaultSections.AddRange(new[]
                {
                    new SectionConfigItem { Type = SectionType.Banner, IsEnabled = true, SortOrder = 0 },
                    new SectionConfigItem { Type = SectionType.Intro, IsEnabled = true, SortOrder = 1 },
                    new SectionConfigItem { Type = SectionType.NavigationButtons, IsEnabled = false, SortOrder = 2 },
                    new SectionConfigItem { Type = SectionType.FeatureSections, IsEnabled = true, SortOrder = 3 },
                    new SectionConfigItem { Type = SectionType.FaqSections, IsEnabled = false, SortOrder = 4 },
                    new SectionConfigItem { Type = SectionType.MemberLogos, IsEnabled = true, SortOrder = 5 }
                });
                break;
        }

        return defaultSections;
    }

    private string GetContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }

    public async Task<List<PageImageViewModel>> GetPageImagesAsync(string pageKey)
    {
        try
        {
            var resp = await _http.GetAsync($"api/content/{pageKey}/images");
            if (!resp.IsSuccessStatusCode) return new List<PageImageViewModel>();
            var json = await resp.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var list = JsonSerializer.Deserialize<List<PageImageViewModel>>(json, options);
            return list ?? new List<PageImageViewModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid hämtning av bilder för {PageKey}", pageKey);
            return new List<PageImageViewModel>();
        }
    }

    public void InvalidateActionPlanCache(string pageKey)
    {
        // Rensa alla actionplan-cacher + sidrelaterad cache för säkerhets skull
        _cache.InvalidateGroup("actionplans");
        _cache.InvalidatePageCache(pageKey);
        // Rensa nyckeln om den skulle vara satt utan grupp
        _cache.InvalidateKey($"actionplan_{pageKey}");
    }

    public async Task<List<PageImageViewModel>> GetAllImagesAsync(string? pageKey = null)
    {
        var url = string.IsNullOrWhiteSpace(pageKey) ? "api/content/images" : $"api/content/images?pageKey={pageKey}";
        return await _http.GetFromJsonAsync<List<PageImageViewModel>>(url) ?? new();
    }

    public sealed class ImageUsageItem
    {
        public int Id { get; set; }
        public string PageKey { get; set; } = "";
        public string? AltText { get; set; }
    }

    public async Task<List<ImageUsageItem>> GetImageUsageAsync(string url)
    {
        var encoded = Uri.EscapeDataString(url);
        return await _http.GetFromJsonAsync<List<ImageUsageItem>>($"api/content/images/usage?url={encoded}") ?? new();
    }

    public async Task<PageImageViewModel?> RegisterExistingImageAsync(string pageKey, string sourceUrl, string altText)
    {
        // Normalize: absolute => relative; trim leading '/'
        if (Uri.TryCreate(sourceUrl, UriKind.Absolute, out var abs))
            sourceUrl = abs.AbsolutePath;
        var sourcePath = (sourceUrl ?? "").TrimStart('/');

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            _logger.LogWarning("RegisterExistingImageAsync: tom SourcePath för {PageKey}", pageKey);
            return null;
        }

        // If the source is not already under this page's folder, copy it; otherwise just register reference
        var targetPrefix = $"images/pages/{pageKey}/";
        var needsCopy = !sourcePath.StartsWith(targetPrefix, StringComparison.OrdinalIgnoreCase);

        var endpoint = needsCopy
            ? $"api/content/{pageKey}/images/register-copy"
            : $"api/content/{pageKey}/images/register";

        var payload = new { SourcePath = sourcePath, AltText = altText };
        var res = await _http.PostAsJsonAsync(endpoint, payload);

        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadAsStringAsync();
            _logger.LogWarning("RegisterExistingImageAsync misslyckades ({Status}): {Message}", res.StatusCode, err);
            return null;
        }

        return await res.Content.ReadFromJsonAsync<PageImageViewModel>();
    }
}