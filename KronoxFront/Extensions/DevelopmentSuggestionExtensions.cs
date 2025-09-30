using KronoxFront.DTOs;
using KronoxFront.ViewModels;

namespace KronoxFront.Extensions;

public static class DevelopmentSuggestionExtensions
{
    public static CreateDevelopmentSuggestionDto ToCreateDto(this DevelopmentSuggestionFormViewModel formModel)
    {
        return new CreateDevelopmentSuggestionDto
        {
            Organization = formModel.Organization?.Trim() ?? string.Empty,
            Name = formModel.Name?.Trim() ?? string.Empty,
            Email = formModel.Email?.Trim() ?? string.Empty,
            Requirement = formModel.Requirement?.Trim() ?? string.Empty,
            ExpectedBenefit = formModel.ExpectedBenefit?.Trim() ?? string.Empty,
            AdditionalInfo = formModel.AdditionalInfo?.Trim() ?? string.Empty
        };
    }

    public static DevelopmentSuggestionViewModel ToViewModel(this DevelopmentSuggestionDto dto)
    {
        return new DevelopmentSuggestionViewModel
        {
            Id = dto.Id,
            Organization = dto.Organization,
            Name = dto.Name,
            Email = dto.Email,
            Requirement = dto.Requirement,
            ExpectedBenefit = dto.ExpectedBenefit,
            AdditionalInfo = dto.AdditionalInfo,
            SubmittedAt = dto.SubmittedAt,
            IsProcessed = dto.IsProcessed,
            ProcessedBy = dto.ProcessedBy,
            ProcessedAt = dto.ProcessedAt
        };
    }

    public static List<DevelopmentSuggestionViewModel> ToViewModels(this List<DevelopmentSuggestionDto> dtos)
    {
        return dtos.Select(dto => dto.ToViewModel()).ToList();
    }
}