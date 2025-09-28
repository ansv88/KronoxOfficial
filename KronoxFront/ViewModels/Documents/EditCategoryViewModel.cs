using System.ComponentModel.DataAnnotations;

namespace KronoxFront.ViewModels.Documents;

public class EditCategoryViewModel
{
    [Required(ErrorMessage = "Kategorinamn måste anges")]
    [StringLength(255, ErrorMessage = "Namnet får vara max 255 tecken")]
    public string Name { get; set; } = "";

    public List<string> AllowedRoles { get; set; } = new();
}