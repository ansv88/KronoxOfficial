using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.Forms;
using KronoxFront.DTOs;
using KronoxFront.ViewModels;
using KronoxFront.Requests;
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

    // Hämtar alla dokument
    public Task<List<DocumentViewModel>> GetDocumentsAsync()
    => _http.GetFromJsonAsync<List<DocumentViewModel>>("api/documents")!;

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

            form.Add(new StringContent("Admin"), "AllowedRoles");

            var res = await _http.PostAsync("api/documents/upload", form);
            return res.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid uppladdning av dokument");
            return false;
        }
    }

    // Sparar ändringar: arkivera/unarkivera + nya kategorikopplingar.
    public Task<bool> SaveDocumentChangesAsync(int id, List<int> categoryIds)
        => _http.PutAsJsonAsync(
                $"api/documents/{id}",
                new { CategoryIds = categoryIds }
           )
           .ContinueWith(t => t.Result.IsSuccessStatusCode);

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

    // Hämtar alla dokument som aktuell användare har åtkomst till.
    public Task<List<DocumentViewModel>> GetAccessibleDocumentsAsync()
        => _http.GetFromJsonAsync<List<DocumentViewModel>>("api/documents/accessible")!;

    // Hämtar dokument kopplade till en specifik kategori (kontrollerar åtkomst per kategori).
    public Task<List<DocumentViewModel>> GetDocumentsByCategoryAsync(int categoryId)
        => _http.GetFromJsonAsync<List<DocumentViewModel>>(
               $"api/documents/by-category/{categoryId}"
           )!;

    // Återställer alla kategorikopplingar (adminfunktion).
    public Task<bool> ResetCategoryRelationshipsAsync()
        => _http.PostAsync("api/category/reset-all", null!)
                .ContinueWith(t => t.Result.IsSuccessStatusCode);
}