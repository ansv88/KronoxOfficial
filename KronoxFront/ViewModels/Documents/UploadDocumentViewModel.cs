using KronoxFront.DTOs;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;

namespace KronoxFront.ViewModels.Documents;

public class UploadDocumentViewModel
{
    [Required(ErrorMessage = "Dokument m�ste v�ljas")]
    public IBrowserFile? File { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Huvudkategori kr�vs")]
    public int SelectedMainCategoryId { get; set; } = 0;

    public List<SubCategoryDto> SelectedSubCategoryIds { get; set; } = new();
}