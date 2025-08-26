using KronoxFront.DTOs;
using KronoxFront.ViewModels;

namespace KronoxFront.Extensions;

public static class DocumentExtensions
{
    // Mappar från API DTO till ViewModel
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
            SubCategories = dto.SubCategories,
            MainCategoryDto = dto.MainCategory, // Direkt mapping eftersom strukturen matchar
            SubCategoryDtos = dto.SubCategoryDtos,
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