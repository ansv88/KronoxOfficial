using KronoxApi.DTOs;
using KronoxApi.Models;

namespace KronoxApi.Extensions;

public static class CustomPageExtensions
{
    public static CustomPageDto ToDto(this CustomPage model)
    {
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
            RequiredRoles = model.RequiredRoles
        };
    }
}