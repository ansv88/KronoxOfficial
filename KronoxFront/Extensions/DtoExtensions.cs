using System.Text.Json;
using KronoxFront.ViewModels;

namespace KronoxFront.Extensions;

// Extension-metoder f�r att deserialisera JSON-str�ngar till ViewModels.

public static class DtoExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    // Mappningar fr�n API-svar (JSON) till viewmodels
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
                // S�kerst�ll att URL inte �r tom
                if (string.IsNullOrEmpty(result.Url))
                {
                    return null;
                }
            }

            return result;
        }
        catch (JsonException ex)
        {
            // Logga felet om m�jligt
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
}