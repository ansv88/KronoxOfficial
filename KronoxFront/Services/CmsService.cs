using KronoxFront.DTOs;
using KronoxFront.Extensions;
using KronoxFront.ViewModels;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace KronoxFront.Services
{
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
        public async Task<bool> UpdatePageImageAltTextAsync(string pageKey, PageImageViewModel image)
        {
            _logger.LogInformation("Uppdaterar alt-text för bild {ImageId} på sida {PageKey}", image.Id, pageKey);

            // Skapa en enkel DTO för att skicka till API
            var dto = new
            {
                ImageId = image.Id,
                AltText = image.AltText
            };

            // Anropa API endpoint för att uppdatera alt-text
            var response = await _http.PutAsJsonAsync($"api/content/{pageKey}/images/{image.Id}/alttext", dto);
            return response.IsSuccessStatusCode;
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

            // Extrahera FeatureSections från metadata och synkronisera till FeatureSection-tabellen
            try
            {
                if (!string.IsNullOrEmpty(model.Metadata))
                {
                    var metadata = JsonDocument.Parse(model.Metadata);

                    if (metadata.RootElement.TryGetProperty("features", out var featuresElement))
                    {
                        // Skapa en lista av FeatureSectionViewModel-objekt
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

                        // Anropa PUT-endpoint för att synkronisera
                        var syncContent = new StringContent(
                            JsonSerializer.Serialize(featureSections),
                            Encoding.UTF8,
                            "application/json");

                        try
                        {
                            var syncResp = await _http.PutAsync($"api/features/{pageKey}", syncContent);

                            if (!syncResp.IsSuccessStatusCode)
                            {
                                _logger.LogWarning(
                                    "Kunde inte synkronisera FeatureSections. Status: {Status}, Innehåll: {Content}",
                                    syncResp.StatusCode,
                                    await syncResp.Content.ReadAsStringAsync());
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
            _logger.LogInformation("Laddar upp sidbild {FileName} för sida {PageKey}", fileName, pageKey);
            using var form = new MultipartFormDataContent();
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(fileName));
            form.Add(fileContent, "file", fileName);
            form.Add(new StringContent(altText), "altText");

            var resp = await _http.PostAsync($"api/content/{pageKey}/images", form);
            if (!resp.IsSuccessStatusCode) return null;
            return (await resp.Content.ReadAsStringAsync()).ToImageViewModel();
        }

        public async Task DeletePageImageAsync(string pageKey, int imageId)
        {
            _logger.LogInformation("Tar bort sidbild {ImageId} från sida {PageKey}", imageId, pageKey);
            var resp = await _http.DeleteAsync($"api/content/{pageKey}/images/{imageId}");
            resp.EnsureSuccessStatusCode();
        }

        public async Task<PageImageViewModel?> RegisterPageImageMetadataAsync(
    string pageKey,
    string sourcePath,
    string altText,
    bool preserveFilename = false)
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

                    // Format 1: "introSection" objekt
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

                    // Format 2: Separata "introTitle" och "introContent" fält
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

                // Felhantering
                return new IntroSectionViewModel
                {
                    Title = "Välkommen",
                    Content = "<p>Ett fel uppstod vid hämtning av innehållet.</p>"
                };
            }
        }

        public async Task<List<HomeFeatureSectionViewModel>> GetHomeFeatureSectionsAsync(string pageKey = "home")
        {
            try
            {
                // Hämta först från features-API
                var response = await _http.GetAsync($"api/features/{pageKey}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var apiFeatures = JsonSerializer.Deserialize<List<FeatureSectionViewModel>>(content, options);

                    if (apiFeatures != null && apiFeatures.Any())
                    {
                        _logger.LogInformation("Hämtade {Count} feature-sektioner från API", apiFeatures.Count);

                        // Konvertera från API-formatet till frontend-format
                        return apiFeatures.Select(f => new HomeFeatureSectionViewModel
                        {
                            Title = f.Title,
                            Content = f.Content,
                            ImageUrl = f.ImageUrl,
                            ImageAltText = string.IsNullOrEmpty(f.ImageAltText) ? f.Title : f.ImageAltText,
                            HasImage = f.ImageUrl != null && f.ImageUrl.Length > 0
                        }).ToList();
                    }
                }
                else
                {
                    _logger.LogWarning("Kunde inte hämta feature-sektioner från API. Status: {Status}",
                        response.StatusCode);
                }

                // Fallback till metadata i ContentBlock
                var page = await GetPageContentAsync(pageKey);
                if (page != null && !string.IsNullOrEmpty(page.Metadata))
                {
                    try
                    {
                        var metadata = JsonDocument.Parse(page.Metadata);
                        var root = metadata.RootElement;

                        if (root.TryGetProperty("features", out var featuresEl))
                        {
                            var features = new List<HomeFeatureSectionViewModel>();

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

                                features.Add(new HomeFeatureSectionViewModel
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
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "Fel vid tolkning av features från metadata: {Message}", ex.Message);
                    }
                }

                // Om inga sektioner hittades
                return new List<HomeFeatureSectionViewModel>
        {
            new HomeFeatureSectionViewModel
            {
                Title = "Innehållet laddas",
                Content = "<p>Innehållet för denna sida är under uppbyggnad.</p>",
                HasImage = false
            }
        };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fel vid hämtning av feature-sektioner: {Message}", ex.Message);

                // Vid fel, returnera en enkel indikation
                return new List<HomeFeatureSectionViewModel>
        {
            new HomeFeatureSectionViewModel
            {
                Title = "Ett fel uppstod",
                Content = "<p>Det gick inte att hämta innehållet. Vänligen försök igen senare.</p>",
                HasImage = false
            }
        };
            }
        }

        // Hjälpmetod för att konvertera till admin-format
        public List<FeatureSectionViewModel> ConvertToAdminFeatureSections(
            List<HomeFeatureSectionViewModel> sections)
        {
            return sections.Select((fs, i) => new FeatureSectionViewModel
            {
                Id = 0, // Nya sektioner får ID 0
                PageKey = "home", // Sätt PageKey
                Title = fs.Title,
                Content = fs.Content,
                ImageUrl = fs.ImageUrl,
                ImageAltText = fs.ImageAltText,
                HasImage = fs.HasImage,
                SortOrder = i
            }).ToList();
        }


        // ---------------------------------------------------
        // FEATURE-SEKTIONSBILDER
        // ---------------------------------------------------
        public async Task<PageImageViewModel?> UploadFeatureImageAsync(
    string pageKey,
    Stream fileStream,
    string fileName,
    string sectionIndex 
)
        {
            _logger.LogInformation("Laddar upp feature-bild {FileName} för sektion {SectionIndex} på sida {PageKey}",
                fileName, sectionIndex, pageKey);

            using var form = new MultipartFormDataContent();
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(fileName));

            form.Add(fileContent, "file", fileName);
            // i API:t heter DTO:t PageImageUploadDto: File, AltText, PreserveFilename
            form.Add(new StringContent(sectionIndex), "altText");
            form.Add(new StringContent("true"), "preserveFilename");

            var response = await _http.PostAsync($"api/content/{pageKey}/images", form);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var dto = json.ToImageViewModel();

            if (dto != null && !dto.Url.StartsWith("/"))
                dto.Url = "/" + dto.Url;

            return dto;
        }

        // ---------------------------------------------------
        // MEDLEMSLOGOTYPER
        // ---------------------------------------------------
        // Hämta
        public async Task<List<MemberLogoViewModel>> GetMemberLogosAsync()
        {
            var response = await _http.GetAsync("api/cms/logos");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return json.ToLogoViewModels() ?? new();
        }

        // Fil-upload
        public async Task<MemberLogoViewModel?> UploadLogoAsync(MemberLogoUploadDto dto)
        {

            using var content = new MultipartFormDataContent();

            var stream = dto.File.OpenReadStream(maxAllowedSize: 10_000_000);
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(dto.File.ContentType));
            content.Add(fileContent, "File", dto.File.Name);
            content.Add(new StringContent(dto.AltText), "AltText");
            content.Add(new StringContent(dto.SortOrd.ToString()), "SortOrd");
            content.Add(new StringContent(dto.LinkUrl), "LinkUrl");

            // kalla på "upload"
            var resp = await _http.PostAsync("api/cms/logos/upload", content);
            if (!resp.IsSuccessStatusCode)
            {
                var errorJson = await resp.Content.ReadAsStringAsync();
                Console.Error.WriteLine($"Upload failed: {errorJson}");
                throw new ApplicationException($"Upload failed: {errorJson}");
            }
            var json = await resp.Content.ReadAsStringAsync();
            return json.ToLogoViewModel();
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

            // 1) Medlemslogotyper
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

            // 2) Sida- och feature-bilder (”home”)
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
}