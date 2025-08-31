using KronoxFront.DTOs;
using KronoxFront.ViewModels;

namespace KronoxFront.Extensions;

public static class DevelopmentSuggestionExtensions
{
    public static CreateDevelopmentSuggestionDto ToCreateDto(this DevelopmentSuggestionFormViewModel formModel)
    {
        return new CreateDevelopmentSuggestionDto
        {
            Organization = formModel.Organization,
            Name = formModel.Name,
            Email = formModel.Email,
            Requirement = formModel.Requirement,
            ExpectedBenefit = formModel.ExpectedBenefit,
            AdditionalInfo = formModel.AdditionalInfo
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