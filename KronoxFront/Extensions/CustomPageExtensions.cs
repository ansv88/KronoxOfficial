using KronoxFront.DTOs;
using KronoxFront.ViewModels;

namespace KronoxFront.Extensions;

public static class CustomPageExtensions
{
    public static CustomPageViewModel ToViewModel(this CustomPageDto dto)
    {
        return new CustomPageViewModel
        {
            Id = dto.Id,
            PageKey = dto.PageKey,
            Title = dto.Title,
            DisplayName = dto.DisplayName,
            Description = dto.Description,
            IsActive = dto.IsActive,
            ShowInNavigation = dto.ShowInNavigation,
            NavigationType = dto.NavigationType,
            ParentPageKey = dto.ParentPageKey,
            SortOrder = dto.SortOrder,
            CreatedAt = dto.CreatedAt,
            LastModified = dto.LastModified,
            CreatedBy = dto.CreatedBy,
            RequiredRoles = dto.RequiredRoles ?? new List<string>()
        };
    }

    public static List<CustomPageViewModel> ToViewModels(this List<CustomPageDto> dtos)
    {
        return dtos.Select(dto => dto.ToViewModel()).ToList();
    }

    public static NavigationPageViewModel ToNavigationViewModel(this NavigationPageDto dto)
    {
        return new NavigationPageViewModel
        {
            PageKey = dto.PageKey,
            DisplayName = dto.DisplayName,
            NavigationType = dto.NavigationType,
            ParentPageKey = dto.ParentPageKey,
            SortOrder = dto.SortOrder,
            RequiredRoles = dto.RequiredRoles ?? new List<string>(),
            Children = dto.Children.Select(c => c.ToNavigationViewModel()).ToList()
        };
    }

    public static List<NavigationPageViewModel> ToNavigationViewModels(this List<NavigationPageDto> dtos)
    {
        return dtos.Select(dto => dto.ToNavigationViewModel()).ToList();
    }

    public static object ToCreateRequest(this CustomPageViewModel viewModel)
    {
        return new
        {
            PageKey = viewModel.PageKey?.Trim() ?? string.Empty,
            Title = viewModel.Title?.Trim() ?? string.Empty,
            DisplayName = viewModel.DisplayName?.Trim() ?? string.Empty,
            Description = viewModel.Description?.Trim() ?? string.Empty,
            IsActive = viewModel.IsActive,
            ShowInNavigation = viewModel.ShowInNavigation,
            NavigationType = viewModel.NavigationType?.Trim() ?? string.Empty,
            ParentPageKey = string.IsNullOrWhiteSpace(viewModel.ParentPageKey) ? null : viewModel.ParentPageKey!.Trim(),
            SortOrder = viewModel.SortOrder,
            RequiredRoles = viewModel.RequiredRoles ?? new List<string>()
        };
    }

    public static object ToUpdateRequest(this CustomPageViewModel viewModel)
    {
        return new
        {
            Title = viewModel.Title?.Trim() ?? string.Empty,
            DisplayName = viewModel.DisplayName?.Trim() ?? string.Empty,
            Description = viewModel.Description?.Trim() ?? string.Empty,
            IsActive = viewModel.IsActive,
            ShowInNavigation = viewModel.ShowInNavigation,
            NavigationType = viewModel.NavigationType?.Trim() ?? string.Empty,
            ParentPageKey = string.IsNullOrWhiteSpace(viewModel.ParentPageKey) ? null : viewModel.ParentPageKey!.Trim(),
            SortOrder = viewModel.SortOrder,
            RequiredRoles = viewModel.RequiredRoles ?? new List<string>()
        };
    }

    public static bool IsAuthorized(this NavigationPageDto page, bool isAuthenticated, System.Security.Claims.ClaimsPrincipal user)
    {
        if (page.RequiredRoles is null || page.RequiredRoles.Count == 0)
            return true;

        if (!isAuthenticated || user is null)
            return false;

        return page.RequiredRoles.Any(role => user.IsInRole(role));
    }

    public static bool IsAuthorized(this NavigationPageViewModel page, bool isAuthenticated, System.Security.Claims.ClaimsPrincipal user)
    {
        if (page.RequiredRoles is null || page.RequiredRoles.Count == 0)
            return true;

        if (!isAuthenticated || user is null)
            return false;

        return page.RequiredRoles.Any(role => user.IsInRole(role));
    }
}