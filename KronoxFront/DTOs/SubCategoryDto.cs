using System.ComponentModel.DataAnnotations;

namespace KronoxFront.DTOs;

public class SubCategoryDto
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Namn på underkategori krävs")]
    public string Name { get; set; } = string.Empty;
}