using KronoxFront.DTOs;

namespace KronoxFront.Services;

// Tjänst för att hantera API-anrop för huvud- och underkategorier av dokument.
// Ger CRUD-operationer mot backend-API för kategorier.
public class CategoryService
{
    private readonly HttpClient _http;
    private readonly UserRolesHeaderService _rolesHeader;

    public CategoryService(HttpClient http, UserRolesHeaderService rolesHeader)
    {
        _http = http;
        _rolesHeader = rolesHeader;
    }

    public async Task<List<MainCategoryDto>> GetMainCategoriesAsync()
    {
        await _rolesHeader.ApplyToAsync(_http);
        return (await _http.GetFromJsonAsync<List<MainCategoryDto>>("api/category/main"))!;
    }

    // Hämtar kategorier baserat på användarens roller
    public async Task<List<MainCategoryDto>> GetAccessibleMainCategoriesAsync()
    {
        await _rolesHeader.ApplyToAsync(_http);
        return (await _http.GetFromJsonAsync<List<MainCategoryDto>>("api/category/main/accessible"))!;
    }

    public async Task<List<SubCategoryDto>> GetSubCategoriesAsync()
    {
        await _rolesHeader.ApplyToAsync(_http);
        return (await _http.GetFromJsonAsync<List<SubCategoryDto>>("api/category/sub"))!;
    }

    public async Task<MainCategoryDto> GetMainCategoryByIdAsync(int id)
    {
        await _rolesHeader.ApplyToAsync(_http);
        return (await _http.GetFromJsonAsync<MainCategoryDto>($"api/category/main/{id}"))!;
    }

    public async Task<SubCategoryDto> GetSubCategoryByIdAsync(int id)
    {
        await _rolesHeader.ApplyToAsync(_http);
        return (await _http.GetFromJsonAsync<SubCategoryDto>($"api/category/sub/{id}"))!;
    }

    public async Task<bool> AddMainCategoryAsync(string name, List<string> allowedRoles)
    {
        await _rolesHeader.ApplyToAsync(_http);
        var response = await _http.PostAsJsonAsync("api/category/main", new { Name = name, AllowedRoles = allowedRoles });
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> AddSubCategoryAsync(string name)
    {
        await _rolesHeader.ApplyToAsync(_http);
        var response = await _http.PostAsJsonAsync("api/category/sub", new { Name = name });
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> EditMainCategoryAsync(int id, string name, List<string> allowedRoles)
    {
        await _rolesHeader.ApplyToAsync(_http);
        var response = await _http.PutAsJsonAsync($"api/category/main/{id}", new { Name = name, AllowedRoles = allowedRoles });
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> EditSubCategoryAsync(int id, string name)
    {
        await _rolesHeader.ApplyToAsync(_http);
        var response = await _http.PutAsJsonAsync($"api/category/sub/{id}", new { Name = name });
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteMainCategoryAsync(int id)
    {
        await _rolesHeader.ApplyToAsync(_http);
        var response = await _http.DeleteAsync($"api/category/main/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteSubCategoryAsync(int id)
    {
        await _rolesHeader.ApplyToAsync(_http);
        var response = await _http.DeleteAsync($"api/category/sub/{id}");
        return response.IsSuccessStatusCode;
    }
}