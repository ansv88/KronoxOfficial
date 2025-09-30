using KronoxFront.DTOs;
using KronoxFront.ViewModels;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace KronoxFront.Extensions;

// Extension-metoder för att deserialisera JSON-strängar till ViewModels.
public static class DtoExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Mappningar från API-svar (JSON) till viewmodels
    public static PageContentViewModel? ToViewModel(this string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            return JsonSerializer.Deserialize<PageContentViewModel>(json, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public static List<MemberLogoViewModel>? ToLogoViewModels(this string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            return JsonSerializer.Deserialize<List<MemberLogoViewModel>>(json, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public static MemberLogoViewModel? ToLogoViewModel(this string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            return JsonSerializer.Deserialize<MemberLogoViewModel>(json, _jsonOptions);
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
            LinkUrl = dto.LinkUrl ?? string.Empty
        };
    }

    // ILogger är valfri för att undvika breaking changes. Logga endast om logger != null.
    public static PageImageViewModel? ToImageViewModel(this string json, ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            var result = JsonSerializer.Deserialize<PageImageViewModel>(json, _jsonOptions);

            // Extra validering
            if (result != null)
            {
                var urlTrimmed = result.Url?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(urlTrimmed))
                    return null;

                result.Url = urlTrimmed;
            }

            return result;
        }
        catch (JsonException ex)
        {
            logger?.LogDebug(ex, "JSON-fel i ToImageViewModel");
            return null;
        }
        catch (Exception ex)
        {
            logger?.LogDebug(ex, "Oväntat fel i ToImageViewModel");
            return null;
        }
    }

    public static T? DeserializeAnonymous<T>(this string json) where T : class, new()
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

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
        if (string.IsNullOrWhiteSpace(json)) return new List<FaqSectionViewModel>();

        try
        {
            var dtos = JsonSerializer.Deserialize<List<FaqSectionDto>>(json, _jsonOptions);

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