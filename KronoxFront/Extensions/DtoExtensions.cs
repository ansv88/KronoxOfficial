using System.Text.Json;
using KronoxFront.ViewModels;
using KronoxFront.DTOs;

namespace KronoxFront.Extensions;

// Extension-metoder för att deserialisera JSON-strängar till ViewModels.

public static class DtoExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    // Mappningar från API-svar (JSON) till viewmodels
    public static PageContentViewModel? ToViewModel(this string json)
    {
        if (string.IsNullOrEmpty(json))
            return null;
            
        try
        {
            var apiDto = JsonSerializer.Deserialize<PageContentViewModel>(json, _jsonOptions);
            return apiDto;
        }
        catch
        {
            return null;
        }
    }

    public static List<MemberLogoViewModel>? ToLogoViewModels(this string json)
    {
        if (string.IsNullOrEmpty(json))
            return null;
            
        try
        {
            var viewModels = JsonSerializer.Deserialize<List<MemberLogoViewModel>>(json, _jsonOptions);
            return viewModels;
        }
        catch
        {
            return null;
        }
    }

    public static MemberLogoViewModel? ToLogoViewModel(this string json)
    {
        if (string.IsNullOrEmpty(json))
            return null;
            
        try
        {
            var viewModel = JsonSerializer.Deserialize<MemberLogoViewModel>(json, _jsonOptions);
            return viewModel;
        }
        catch
        {
            return null;
        }
    }

    public static MemberLogoViewModel ToViewModel(this MemberLogoDto dto)
    {
        return new MemberLogoViewModel
        {
            Id = dto.Id,
            Url = dto.Url,
            AltText = dto.AltText,
            SortOrd = dto.SortOrd,
            LinkUrl = dto.LinkUrl
        };
    }

    public static PageImageViewModel? ToImageViewModel(this string json)
    {
        try
        {
            if (string.IsNullOrEmpty(json))
                return null;

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<PageImageViewModel>(json, options);

            // Extra validering och logging
            if (result != null)
            {
                // Säkerställ att URL inte är tom
                if (string.IsNullOrEmpty(result.Url))
                {
                    return null;
                }
            }

            return result;
        }
        catch (JsonException ex)
        {
            // Logga felet om möjligt
            System.Diagnostics.Debug.WriteLine($"JSON deserialization error in ToImageViewModel: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Unexpected error in ToImageViewModel: {ex.Message}");
            return null;
        }
    }

    public static T? DeserializeAnonymous<T>(this string json) where T : class, new()
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public static List<FaqSectionViewModel> ToFaqSectionViewModels(this string json)
    {
        if (string.IsNullOrEmpty(json)) return new List<FaqSectionViewModel>();
        
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dtos = JsonSerializer.Deserialize<List<FaqSectionDto>>(json, options);
            
            return dtos?.Select(dto => new FaqSectionViewModel
            {
                Id = dto.Id,
                PageKey = dto.PageKey,
                Title = dto.Title,
                Description = dto.Description,
                SortOrder = dto.SortOrder,
                FaqItems = dto.FaqItems.Select(item => new FaqItemViewModel
                {
                    Id = item.Id,
                    FaqSectionId = item.FaqSectionId,
                    Question = item.Question,
                    Answer = item.Answer,
                    ImageUrl = item.ImageUrl,
                    ImageAltText = item.ImageAltText,
                    HasImage = item.HasImage,
                    SortOrder = item.SortOrder
                }).ToList()
            }).ToList() ?? new List<FaqSectionViewModel>();
        }
        catch
        {
            return new List<FaqSectionViewModel>();
        }
    }
}