using KronoxFront.DTOs;
using KronoxFront.ViewModels;

namespace KronoxFront.Extensions;

public static class DocumentExtensions
{
    // Mappar fr�n API DTO till ViewModel
    public static DocumentViewModel ToViewModel(this DocumentDto dto)
    {
        return new DocumentViewModel
        {
            Id = dto.Id,
            FileName = dto.FileName,
            FilePath = dto.FilePath,
            UploadedAt = dto.UploadedAt,
            FileSize = dto.FileSize,
            MainCategoryId = dto.MainCategoryId,
            SubCategories = (dto.SubCategories ?? new List<int>()).ToList(),
            MainCategoryDto = dto.MainCategory ?? new MainCategoryDto(),
            SubCategoryDtos = (dto.SubCategoryDtos ?? new List<SubCategoryDto>()).ToList(),
            IsArchived = dto.IsArchived,
            ArchivedAt = dto.ArchivedAt,
            ArchivedBy = dto.ArchivedBy
        };
    }

    // Mappar lista av DTOs till ViewModels
    public static List<DocumentViewModel> ToViewModels(this List<DocumentDto> dtos)
    {
        return dtos.Select(dto => dto.ToViewModel()).ToList();
    }
}