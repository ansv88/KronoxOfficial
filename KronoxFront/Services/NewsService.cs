using KronoxFront.DTOs;
using KronoxFront.ViewModels;
using System.Text;
using System.Text.Json;

namespace KronoxFront.Services;

public class NewsService
{
    private readonly HttpClient _http;
    private readonly ILogger<NewsService> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    public NewsService(HttpClient http, ILogger<NewsService> logger)
    {
        _http = http;
        _logger = logger;
    }

    // Hämta nyheter för medlemmar (publika sidan)
    public async Task<NewsListViewModel> GetMemberNewsAsync(bool includeArchived = false, int page = 1, int pageSize = 10)
    {
        try
        {
            var response = await _http.GetAsync($"api/news/member-news?includeArchived={includeArchived}&page={page}&pageSize={pageSize}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var newsResponse = JsonSerializer.Deserialize<NewsApiResponse>(json, _jsonOptions);

                if (newsResponse != null)
                {
                    return new NewsListViewModel
                    {
                        Posts = newsResponse.Posts.Select(MapToViewModel).ToList(),
                        TotalCount = newsResponse.TotalCount,
                        Page = newsResponse.Page,
                        PageSize = newsResponse.PageSize,
                        TotalPages = newsResponse.TotalPages
                    };
                }
            }

            _logger.LogWarning("Kunde inte hämta medlemsnyheter: {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid hämtning av medlemsnyheter");
        }

        return new NewsListViewModel();
    }

    // Hämta alla nyheter (admin)
    public async Task<List<NewsItemViewModel>> GetAllNewsAsync()
    {
        try
        {
            var response = await _http.GetAsync("api/news/all");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var newsItems = JsonSerializer.Deserialize<List<NewsApiItem>>(json, _jsonOptions);

                return newsItems?.Select(MapToViewModel).ToList() ?? new List<NewsItemViewModel>();
            }

            _logger.LogWarning("Kunde inte hämta alla nyheter: {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid hämtning av alla nyheter");
        }

        return new List<NewsItemViewModel>();
    }

    // Hämta specifik nyhet
    public async Task<NewsItemViewModel?> GetNewsAsync(int id)
    {
        try
        {
            var response = await _http.GetAsync($"api/news/{id}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var newsItem = JsonSerializer.Deserialize<NewsApiItem>(json, _jsonOptions);

                return newsItem != null ? MapToViewModel(newsItem) : null;
            }

            _logger.LogWarning("Kunde inte hämta nyhet {Id}: {StatusCode}", id, response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid hämtning av nyhet {Id}", id);
        }

        return null;
    }

    // Skapa ny nyhet (admin)
    public async Task<NewsItemViewModel?> CreateNewsAsync(CreateNewsViewModel model)
    {
        try
        {
            var request = new
            {
                Title = model.Title,
                Content = model.Content,
                ScheduledPublishDate = model.ScheduledPublishDate,
                IsArchived = model.IsArchived,
                VisibleToRoles = string.Join(",", model.SelectedRoles)
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("api/news/create", content);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var newsItem = JsonSerializer.Deserialize<NewsApiItem>(responseJson, _jsonOptions);

                return newsItem != null ? MapToViewModel(newsItem) : null;
            }

            _logger.LogWarning("Kunde inte skapa nyhet: {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid skapande av nyhet");
        }

        return null;
    }

    // Uppdatera nyhet (admin)
    public async Task<NewsItemViewModel?> UpdateNewsAsync(int id, CreateNewsViewModel model)
    {
        try
        {
            var request = new
            {
                Title = model.Title,
                Content = model.Content,
                ScheduledPublishDate = model.ScheduledPublishDate,
                IsArchived = model.IsArchived,
                VisibleToRoles = string.Join(",", model.SelectedRoles)
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PutAsync($"api/news/update/{id}", content);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var newsItem = JsonSerializer.Deserialize<NewsApiItem>(responseJson, _jsonOptions);

                return newsItem != null ? MapToViewModel(newsItem) : null;
            }

            _logger.LogWarning("Kunde inte uppdatera nyhet {Id}: {StatusCode}", id, response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid uppdatering av nyhet {Id}", id);
        }

        return null;
    }

    // Ta bort nyhet (admin)
    public async Task<bool> DeleteNewsAsync(int id)
    {
        try
        {
            var response = await _http.DeleteAsync($"api/news/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid borttagning av nyhet {Id}", id);
            return false;
        }
    }

    // Arkivera/avarkivera nyhet (admin)
    public async Task<bool> ToggleArchiveAsync(int id)
    {
        try
        {
            var response = await _http.PutAsync($"api/news/{id}/archive", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid arkivering av nyhet {Id}", id);
            return false;
        }
    }

    public async Task<List<NewsDocumentViewModel>> GetNewsDocumentsAsync(int newsId)
    {
        try
        {
            var response = await _http.GetAsync($"api/news/{newsId}/documents");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var documents = JsonSerializer.Deserialize<List<NewsDocumentViewModel>>(json, _jsonOptions);
                return documents ?? new List<NewsDocumentViewModel>();
            }

            _logger.LogWarning("Kunde inte hämta dokument för nyhet {NewsId}: {StatusCode}", newsId, response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid hämtning av dokument för nyhet {NewsId}", newsId);
        }

        return new List<NewsDocumentViewModel>();
    }

    public async Task<bool> AttachDocumentToNewsAsync(int newsId, int documentId, string? displayName = null, int sortOrder = 0)
    {
        try
        {
            var request = new
            {
                DocumentId = documentId,
                DisplayName = displayName,
                SortOrder = sortOrder
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync($"api/news/{newsId}/documents", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid koppling av dokument till nyhet");
            return false;
        }
    }

    public async Task<bool> DetachDocumentFromNewsAsync(int newsId, int documentId)
    {
        try
        {
            var response = await _http.DeleteAsync($"api/news/{newsId}/documents/{documentId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid borttagning av dokumentkoppling");
            return false;
        }
    }

    private NewsItemViewModel MapToViewModel(NewsApiItem item)
    {
        return new NewsItemViewModel
        {
            Id = item.Id,
            Title = item.Title,
            Content = item.Content,
            PublishedDate = item.PublishedDate,
            ScheduledPublishDate = item.ScheduledPublishDate,
            IsArchived = item.IsArchived,
            VisibleToRoles = item.VisibleToRoles,
            CreatedDate = item.CreatedDate,
            LastModified = item.LastModified,
        };
    }
}