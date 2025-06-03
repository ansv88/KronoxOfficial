using KronoxFront.DTOs;

namespace KronoxFront.Services;

// Tjänst för att hantera API-anrop för huvud- och underkategorier av dokument.
// Ger CRUD-operationer mot backend-API för kategorier.
public class CategoryService
{
    private readonly HttpClient _http;
    public CategoryService(HttpClient http) => _http = http;

    public Task<List<MainCategoryDto>> GetMainCategoriesAsync()
        => _http.GetFromJsonAsync<List<MainCategoryDto>>("api/category/main")!;

    public Task<List<SubCategoryDto>> GetSubCategoriesAsync()
    => _http.GetFromJsonAsync<List<SubCategoryDto>>("api/category/sub")!;

    public Task<MainCategoryDto> GetMainCategoryByIdAsync(int id)
        => _http.GetFromJsonAsync<MainCategoryDto>($"api/category/main/{id}")!;

    public Task<SubCategoryDto> GetSubCategoryByIdAsync(int id)
        => _http.GetFromJsonAsync<SubCategoryDto>($"api/category/sub/{id}")!;

    public Task<bool> AddMainCategoryAsync(string name)
    => _http.PostAsJsonAsync("api/category/main", new { Name = name })
            .ContinueWith(t => t.Result.IsSuccessStatusCode);

    public Task<bool> AddSubCategoryAsync(string name)
    => _http.PostAsJsonAsync("api/category/sub", new { Name = name })
            .ContinueWith(t => t.Result.IsSuccessStatusCode);

    public Task<bool> EditMainCategoryAsync(int id, string name)
        => _http.PutAsJsonAsync($"api/category/main/{id}", new { Name = name })
                .ContinueWith(t => t.Result.IsSuccessStatusCode);

    public Task<bool> EditSubCategoryAsync(int id, string name)
        => _http.PutAsJsonAsync($"api/category/sub/{id}", new { Name = name})
                .ContinueWith(t => t.Result.IsSuccessStatusCode);

    public Task<bool> DeleteMainCategoryAsync(int id)
        => _http.DeleteAsync($"api/category/main/{id}")
                .ContinueWith(t => t.Result.IsSuccessStatusCode);

    public Task<bool> DeleteSubCategoryAsync(int id)
        => _http.DeleteAsync($"api/category/sub/{id}")
                .ContinueWith(t => t.Result.IsSuccessStatusCode);
}