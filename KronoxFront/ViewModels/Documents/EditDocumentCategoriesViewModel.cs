using System.ComponentModel.DataAnnotations;

namespace KronoxFront.ViewModels.Documents;

public class EditDocumentCategoriesViewModel
{
    public int DocumentId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Huvudkategori kr�vs")]
    public int SelectedMainCategoryId { get; set; }

    public List<int> SelectedSubCategoryIds { get; set; } = new();
}