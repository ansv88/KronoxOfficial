using KronoxFront.DTOs;
using KronoxFront.Extensions;
using KronoxFront.ViewModels;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace KronoxFront.Services;

public class CmsService
{
    private readonly HttpClient _http;
    private readonly ILogger<CmsService> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly JsonSerializerOptions _jsonOptions
        = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    public CmsService(HttpClient http, IWebHostEnvironment env, ILogger<CmsService> logger)
    {
        _http = http;
        _env = env;
        _logger = logger;
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
        _logger.LogInformation("Hämtar sidinnehåll för sida: {PageKey}", pageKey);
        var resp = await _http.GetAsync($"api/content/{pageKey}");
        if (!resp.IsSuccessStatusCode) return null;
        return (await resp.Content.ReadAsStringAsync()).ToViewModel();
    }

    public async Task SavePageContentAsync(string pageKey, PageContentViewModel model)
    {
        // Spara via ContentController
        var json = JsonSerializer.Serialize(model);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var resp = await _http.PutAsync($"api/content/{pageKey}", content);
        resp.EnsureSuccessStatusCode();

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
    // FEATURE-SEKTIONSINNEHÅLL
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

                    return new IntroSectionViewModel
                    {
                        Title = title,
                        Content = content,
                        ImageUrl = imageUrl,
                        ImageAltText = imageAltText,
                        HasImage = hasImage
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

    public async Task<List<FeatureSectionViewModel>> GetFeatureSectionsAsync(string pageKey, bool includePrivate = false)
    {
        try
        {
            var endpoint = includePrivate ? $"api/featuresections/{pageKey}/authenticated" : $"api/featuresections/{pageKey}";
            var response = await _http.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();

                if (includePrivate)
                {
                    var sectionsWithPrivate = JsonSerializer.Deserialize<List<FeatureSectionWithPrivateDto>>(json, _jsonOptions) ?? new();
                    return MapToViewModelsWithPrivate(sectionsWithPrivate);
                }
                else
                {
                    // För publika anrop, använd den vanliga DTO:n men med kontaktpersoner
                    var sectionsWithPrivate = JsonSerializer.Deserialize<List<FeatureSectionWithPrivateDto>>(json, _jsonOptions) ?? new();
                    return MapToViewModelsWithPrivate(sectionsWithPrivate, includePrivate: false);
                }
            }

            // Fallback till metadata
            return await GetFeatureSectionsFromMetadata(pageKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid hämtning av feature-sektioner för {PageKey}", pageKey);
            return await GetFeatureSectionsFromMetadata(pageKey);
        }
    }

    // Fallback-metod för att hämta från metadata
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

    // Mapping-metoder
    private List<FeatureSectionViewModel> MapToViewModels(List<FeatureSectionDto> dtos)
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
            HasPrivateContent = dto.HasPrivateContent
        }).ToList();
    }

    // Mapping-metoder
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
            PrivateContent = includePrivate ? dto.PrivateContent : "", // Filtrera privat innehåll
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

            // Synkronisera metadata också
            await UpdateMetadataWithFeatureSections(pageKey, sections);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid sparande av feature-sektioner för {PageKey}", pageKey);
            throw;
        }
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

            // Uppdatera features i metadata
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
                hasImage = introSection.HasImage
            };

            pageContent.Metadata = JsonSerializer.Serialize(existingMetadata);
            pageContent.LastModified = DateTime.Now;

            await SavePageContentAsync(pageKey, pageContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid sparande av intro-sektion för {PageKey}", pageKey);
            throw;
        }
    }

    private string GetDefaultPageTitle(string pageKey)
    {
        return pageKey switch
        {
            "home" => "Startsida",
            "omkonsortiet" => "Om konsortiet",
            "visioner" => "Visioner & Verksamhetsidé",
            _ => "Sida"
        };
    }

    // ---------------------------------------------------
    // FEATURESEKTIONSBILDER
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
    // MEDLEMSLOGOTYPER
    // ---------------------------------------------------
    public async Task<List<MemberLogoViewModel>> GetMemberLogosAsync()
    {
        var response = await _http.GetAsync("api/cms/logos");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return json.ToLogoViewModels() ?? new();
    }

    public async Task<MemberLogoViewModel?> UploadLogoAsync(MemberLogoUploadDto dto)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var stream = dto.File.OpenReadStream(maxAllowedSize: 10_000_000);
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(dto.File.ContentType));
            content.Add(fileContent, "File", dto.File.Name);
            content.Add(new StringContent(dto.AltText), "AltText");
            content.Add(new StringContent(dto.SortOrd.ToString()), "SortOrd");
            content.Add(new StringContent(dto.LinkUrl), "LinkUrl");

            var resp = await _http.PostAsync("api/cms/logos/upload", content);
            if (!resp.IsSuccessStatusCode)
            {
                var errorJson = await resp.Content.ReadAsStringAsync();
                _logger.LogError("Misslyckades med uppladdning av logotyp: {Error}", errorJson);
                throw new ApplicationException($"Upload failed: {errorJson}");
            }
            var json = await resp.Content.ReadAsStringAsync();
            return json.ToLogoViewModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid uppladdning av logotyp");
            throw;
        }
    }

    // Metadata-register (filer som redan ligger i wwwroot/images/members)
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

    public async Task DeleteLogoAsync(int logoId)
    {
        _logger.LogInformation("Tar bort logotyp {LogoId}", logoId);
        var resp = await _http.DeleteAsync($"api/cms/logos/{logoId}");
        resp.EnsureSuccessStatusCode();
    }

    public async Task<bool> MoveLogoAsync(int logoId, int direction)
    {
        var payload = new { LogoId = logoId, Direction = direction };
        var resp = await _http.PostAsJsonAsync("api/cms/logos/move", payload);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateLogoDescriptionAsync(int logoId, string description)
    {
        var resp = await _http.PutAsJsonAsync(
            $"api/cms/logos/{logoId}/description",
            new { Description = description });
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateLogoLinkUrlAsync(int logoId, string linkUrl)
    {
        var resp = await _http.PutAsJsonAsync(
            $"api/cms/logos/{logoId}/link",
            new { LinkUrl = linkUrl });
        return resp.IsSuccessStatusCode;
    }

    // ---------------------------------------------------
    // SYNKRONISERA LOKALA BILDER
    // ---------------------------------------------------
    public async Task<bool> SyncImagesFromApiAsync()
    {
        _logger.LogInformation("Synkroniserar bilder från API till wwwroot");

        // Medlemslogotyper
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
    // HJÄLPARE
    // ---------------------------------------------------
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
}