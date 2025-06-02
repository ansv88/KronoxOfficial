using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.Forms;
using KronoxFront.DTOs;
using KronoxFront.ViewModels;
using KronoxFront.Requests;

namespace KronoxFront.Services;

public class DocumentService
{
    private readonly HttpClient _http;

    public DocumentService(HttpClient http)
        => _http = http;

    // Hämtar alla dokument
    public Task<List<DocumentViewModel>> GetDocumentsAsync()
    => _http.GetFromJsonAsync<List<DocumentViewModel>>("api/documents")!;

    // Laddar upp ett nytt dokument med tillhörande huvudkategorier.
    public async Task<bool> UploadDocumentAsync(IBrowserFile file, int mainCategoryId, List<SubCategoryDto> subCategoryIds)
    {
        using var form = new MultipartFormDataContent();

        // Bildström
        var stream = file.OpenReadStream(25 * 1024 * 1024);
        var content = new StreamContent(stream);
        content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        form.Add(content, "File", file.Name);

        // Huvudkategori
        form.Add(new StringContent(mainCategoryId.ToString()), "MainCategoryId");

        // Underkategorier
        if (subCategoryIds != null)
        {
            foreach (var subCategory in subCategoryIds)
                form.Add(new StringContent(subCategory.Id.ToString()), "SubCategoryIds");
        }

        form.Add(new StringContent("Admin"), "AllowedRoles"); //Eriks hack

        var res = await _http.PostAsync("api/documents/upload", form);
        return res.IsSuccessStatusCode;
    }

    // Sparar ändringar: arkivera/unarkivera + nya kategorikopplingar.
    public Task<bool> SaveDocumentChangesAsync(int id, List<int> categoryIds)
        => _http.PutAsJsonAsync(
                $"api/documents/{id}",
                new { CategoryIds = categoryIds }
           )
           .ContinueWith(t => t.Result.IsSuccessStatusCode);

    // Updates a document's categories
    public async Task<bool> UpdateDocumentAsync(int documentId, UpdateDocumentRequest request)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"api/documents/{documentId}", request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
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
        var res = await _http.GetAsync(
            $"api/documents/{id}",
            HttpCompletionOption.ResponseHeadersRead
        );
        if (!res.IsSuccessStatusCode) return null;

        var stream = await res.Content.ReadAsStreamAsync();

        // Filnamn från Content-Disposition header
        var cd = res.Content.Headers.ContentDisposition;
        var fn = cd?.FileName?.Trim('"') ?? $"document_{id}";
        return (stream, fn);
    }

    // Hämtar alla dokument som aktuell användare har åtkomst till.
    public Task<List<DocumentViewModel>> GetAccessibleDocumentsAsync()
        => _http.GetFromJsonAsync<List<DocumentViewModel>>("api/documents/accessible")!;

    // Hämtar dokument kopplade till en specifik kategori (kontrollerar åtkomst per kategori).
    public Task<List<DocumentViewModel>> GetDocumentsByCategoryAsync(int categoryId)
        => _http.GetFromJsonAsync<List<DocumentViewModel>>(
               $"api/documents/by-category/{categoryId}"
           )!;

    public Task<bool> ResetCategoryRelationshipsAsync()
        => _http.PostAsync("api/category/reset-all", null!)
                .ContinueWith(t => t.Result.IsSuccessStatusCode);
}