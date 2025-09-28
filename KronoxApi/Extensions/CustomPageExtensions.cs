using KronoxApi.DTOs;
using KronoxApi.Models;

namespace KronoxApi.Extensions;

/// <summary>
/// Extensions för att konvertera mellan CustomPage och dess DTO.
/// Håller mappningen ren utan logik eller sidoeffekter.
/// </summary>
public static class CustomPageExtensions
{
    // Mappar en CustomPage-modell till en DTO.
    // <exception cref="ArgumentNullException">Om model är null.</exception>
    public static CustomPageDto ToDto(this CustomPage model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return new CustomPageDto
        {
            Id = model.Id,
            PageKey = model.PageKey,
            Title = model.Title,
            DisplayName = model.DisplayName,
            Description = model.Description,
            IsActive = model.IsActive,
            ShowInNavigation = model.ShowInNavigation,
            NavigationType = model.NavigationType,
            ParentPageKey = model.ParentPageKey,
            SortOrder = model.SortOrder,
            CreatedAt = model.CreatedAt,
            LastModified = model.LastModified,
            CreatedBy = model.CreatedBy,
            RequiredRoles = model.RequiredRoles ?? new()
        };
    }

    // Mappar en sekvens av CustomPage till en lista av DTO:er.
    // Returnerar tom lista om input är null.
    public static List<CustomPageDto> ToDtos(this IEnumerable<CustomPage>? models)
    {
        if (models is null) return new();
        return models.Select(m => m.ToDto()).ToList();
    }
}