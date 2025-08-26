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

    public DocumentService(HttpClient http, ILogger<DocumentService> logger)
    {
        _http = http;
        _logger = logger;
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

    // **NYCKELMETHOD** - Hämtar dokument baserat på användarens roller med korrekt mappning
    public async Task<List<DocumentViewModel>> GetAccessibleDocumentsAsync()
    {
        try
        {
            var documentDtos = await _http.GetFromJsonAsync<List<DocumentDto>>("api/documents/accessible");
            var viewModels = documentDtos?.ToViewModels() ?? new List<DocumentViewModel>();
            
            _logger.LogInformation("Hämtade {Count} tillgängliga dokument från API", viewModels.Count);
            
            // Debug-loggning för att kontrollera rolldata
            foreach (var vm in viewModels.Take(3)) // Logga bara första 3 för att undvika spam
            {
                _logger.LogInformation("Dokument {FileName}: Kategori {CategoryName}, Roller: {Roles}", 
                    vm.FileName, 
                    vm.MainCategoryDto.Name, 
                    string.Join(", ", vm.MainCategoryDto.AllowedRoles));
            }
            
            return viewModels;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid hämtning av tillgängliga dokument");
            return new List<DocumentViewModel>();
        }
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