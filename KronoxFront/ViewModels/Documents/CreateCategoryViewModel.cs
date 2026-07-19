using System.ComponentModel.DataAnnotations;

namespace KronoxFront.ViewModels.Documents;

public class CreateCategoryViewModel
{
    [Required(ErrorMessage = "Kategorinamn måste anges")]
    [StringLength(50, ErrorMessage = "Namnet får vara max 50 tecken")]
    public string Name { get; set; } = "";

    public List<string> AllowedRoles { get; set; } = new();
}