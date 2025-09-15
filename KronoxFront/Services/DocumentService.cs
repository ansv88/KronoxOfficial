using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.Forms;
using KronoxFront.DTOs;
using KronoxFront.ViewModels;
using KronoxFront.Requests;
using KronoxFront.Extensions;
using Microsoft.Extensions.Logging;

namespace KronoxFront.Services;

// Tjänst för att hantera dokumentrelaterade API-anrop, inklusive uppladdning, hämtning, uppdatering och borttagning av dokument.
public class DocumentService
{
    private readonly HttpClient _http;
    private readonly ILogger<DocumentService> _logger;
    private readonly CacheService _cache;

    public DocumentService(HttpClient http, ILogger<DocumentService> logger, CacheService cache)
    {
        _http = http;
        _logger = logger;
        _cache = cache;
    }

    // Hämtar alla dokument (endast admin) med korrekt DTO-mappning
    public async Task<List<DocumentViewModel>> GetDocumentsAsync()
    {
        try
        {
            var documentDtos = await _http.GetFromJsonAsync<List<DocumentDto>>("api/documents");
            return documentDtos?.ToViewModels() ?? new List<DocumentViewModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid hämtning av alla dokument");
            return new List<DocumentViewModel>();
        }
    }

    // Hämtar dokument baserat på användarens roller med korrekt mappning
    public async Task<List<DocumentViewModel>> GetAccessibleDocumentsAsync()
    {
        // Använd befintlig cache-metod från CacheService
        var cachedDocs = await _cache.GetDocumentsAsync(async () =>
        {
            _logger.LogInformation("Fetching accessible documents from API");
            
            try
            {
                var response = await _http.GetAsync("api/documents/accessible");
                if (response.IsSuccessStatusCode)
                {
                    var dtos = await response.Content.ReadFromJsonAsync<List<DocumentDto>>();
                    return dtos?.Select(MapToViewModel).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching accessible documents");
            }
            
            return new List<DocumentViewModel>();
        });
        
        return cachedDocs ?? new List<DocumentViewModel>();
    }

    private DocumentViewModel MapToViewModel(DocumentDto dto)
    {
        return new DocumentViewModel
        {
            Id = dto.Id,
            FileName = dto.FileName,
            FilePath = dto.FilePath,
            UploadedAt = dto.UploadedAt,
            FileSize = dto.FileSize,
            MainCategoryId = dto.MainCategoryId,
            SubCategories = dto.SubCategories ?? new List<int>(),
            IsArchived = dto.IsArchived,
            ArchivedAt = dto.ArchivedAt,
            ArchivedBy = dto.ArchivedBy,
            MainCategory = dto.MainCategory != null ? new MainCategoryViewModel
            {
                Id = dto.MainCategory.Id,
                Name = dto.MainCategory.Name,
                AllowedRoles = dto.MainCategory.AllowedRoles ?? new List<string>(),
                IsActive = dto.MainCategory.IsActive
            } : null,
            MainCategoryDto = dto.MainCategory ?? new MainCategoryDto() // Fallback
        };
    }

    // Hämtar dokument kopplade till en specifik kategori med korrekt mappning
    public async Task<List<DocumentViewModel>> GetDocumentsByCategoryAsync(int categoryId)
    {
        try
        {
            var documentDtos = await _http.GetFromJsonAsync<List<DocumentDto>>($"api/documents/by-maincategory/{categoryId}");
            return documentDtos?.ToViewModels() ?? new List<DocumentViewModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid hämtning av dokument för kategori {CategoryId}", categoryId);
            return new List<DocumentViewModel>();
        }
    }

    // Laddar upp ett nytt dokument med tillhörande huvudkategorier.
    public async Task<bool> UploadDocumentAsync(IBrowserFile file, int mainCategoryId, List<SubCategoryDto> subCategoryIds)
    {
        try
        {
            using var form = new MultipartFormDataContent();

            var stream = file.OpenReadStream(25 * 1024 * 1024);
            var content = new StreamContent(stream);
            content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            form.Add(content, "File", file.Name);

            form.Add(new StringContent(mainCategoryId.ToString()), "MainCategoryId");

            if (subCategoryIds != null)
            {
                foreach (var subCategory in subCategoryIds)
                    form.Add(new StringContent(subCategory.Id.ToString()), "SubCategoryIds");
            }

            var res = await _http.PostAsync("api/documents/upload", form);
            return res.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid uppladdning av dokument");
            return false;
        }
    }

    // Arkiverar ett dokument
    public async Task<bool> ArchiveDocumentAsync(int id)
    {
        try
        {
            var response = await _http.PostAsync($"api/documents/{id}/archive", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid arkivering av dokument {DocumentId}", id);
            return false;
        }
    }

    // Återställer ett arkiverat dokument
    public async Task<bool> UnarchiveDocumentAsync(int id)
    {
        try
        {
            var response = await _http.PostAsync($"api/documents/{id}/unarchive", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid återställning av dokument {DocumentId}", id);
            return false;
        }
    }

    // Uppdaterar ett dokuments kategorier.
    public async Task<bool> UpdateDocumentAsync(int documentId, UpdateDocumentRequest request)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"api/documents/{documentId}", request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid uppdatering av dokument {DocumentId}", documentId);
            return false;
        }
    }

    // Tar bort ett dokument.
    public Task<bool> DeleteDocumentAsync(int id)
        => _http.DeleteAsync($"api/documents/{id}")
                .ContinueWith(t => t.Result.IsSuccessStatusCode);

    // Laddar ner ett dokument som en ström + filnamn.
    public async Task<(Stream content, string fileName)?> DownloadDocumentAsync(int id)
    {
        try
        {
            var res = await _http.GetAsync(
                $"api/documents/{id}",
                HttpCompletionOption.ResponseHeadersRead
            );
            if (!res.IsSuccessStatusCode) return null;

            var stream = await res.Content.ReadAsStreamAsync();

            var cd = res.Content.Headers.ContentDisposition;
            var fn = cd?.FileName?.Trim('"') ?? $"document_{id}";
            return (stream, fn);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid nedladdning av dokument {DocumentId}", id);
            return null;
        }
    }
}