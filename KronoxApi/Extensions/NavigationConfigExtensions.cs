using KronoxApi.DTOs;
using KronoxApi.Models;

namespace KronoxApi.Extensions;

public static class NavigationConfigExtensions
{
    public static NavigationConfigDto ToDto(this NavigationConfig config)
    {
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
            CreatedAt = config.CreatedAt,
            LastModified = config.LastModified
        };
    }

    public static List<NavigationConfigDto> ToDtos(this List<NavigationConfig> configs)
    {
        return configs.Select(config => config.ToDto()).ToList();
    }

    public static NavigationConfig ToModel(this NavigationConfigDto dto)
    {
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
            CreatedAt = dto.CreatedAt,
            LastModified = dto.LastModified
        };
    }

    public static List<NavigationConfig> ToModels(this List<NavigationConfigDto> dtos)
    {
        return dtos.Select(dto => dto.ToModel()).ToList();
    }
}