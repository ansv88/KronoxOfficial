using KronoxApi.DTOs;
using KronoxApi.Models;

namespace KronoxApi.Extensions;

/// <summary>
/// Extensions för att konvertera mellan NavigationConfig och dess DTO.
/// Håller mappningen ren utan logik eller sidoeffekter.
/// </summary>
public static class NavigationConfigExtensions
{
    // Mappar NavigationConfig till NavigationConfigDto.
    // <exception cref="ArgumentNullException">Om config är null.</exception>
    public static NavigationConfigDto ToDto(this NavigationConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return new NavigationConfigDto
        {
            Id = config.Id,
            PageKey = config.PageKey,
            DisplayName = config.DisplayName,
            ItemType = config.ItemType,
            SortOrder = config.SortOrder,
            GuestSortOrder = config.GuestSortOrder,
            MemberSortOrder = config.MemberSortOrder,
            IsVisibleToGuests = config.IsVisibleToGuests,
            IsVisibleToMembers = config.IsVisibleToMembers,
            IsActive = config.IsActive,
            IsSystemItem = config.IsSystemItem,
            RequiredRoles = config.RequiredRoles,
            CreatedAt = config.CreatedAt,
            LastModified = config.LastModified
        };
    }

    // Mappar en sekvens av NavigationConfig till en lista av DTO:er.
    // Returnerar tom lista om input är null.
    public static List<NavigationConfigDto> ToDtos(this IEnumerable<NavigationConfig>? configs)
    {
        if (configs is null) return new();
        return configs.Select(config => config.ToDto()).ToList();
    }

    // Mappar NavigationConfigDto till NavigationConfig-modell.
    // <exception cref="ArgumentNullException">Om dto är null.</exception>
    public static NavigationConfig ToModel(this NavigationConfigDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new NavigationConfig
        {
            Id = dto.Id,
            PageKey = dto.PageKey,
            DisplayName = dto.DisplayName,
            ItemType = dto.ItemType,
            SortOrder = dto.SortOrder,
            GuestSortOrder = dto.GuestSortOrder,
            MemberSortOrder = dto.MemberSortOrder,
            IsVisibleToGuests = dto.IsVisibleToGuests,
            IsVisibleToMembers = dto.IsVisibleToMembers,
            IsActive = dto.IsActive,
            IsSystemItem = dto.IsSystemItem,
            RequiredRoles = dto.RequiredRoles,
            CreatedAt = dto.CreatedAt,
            LastModified = dto.LastModified
        };
    }

    // Mappar en sekvens av NavigationConfigDto till en lista av modeller.
    // Returnerar tom lista om input är null.
    public static List<NavigationConfig> ToModels(this IEnumerable<NavigationConfigDto>? dtos)
    {
        if (dtos is null) return new();
        return dtos.Select(dto => dto.ToModel()).ToList();
    }
}