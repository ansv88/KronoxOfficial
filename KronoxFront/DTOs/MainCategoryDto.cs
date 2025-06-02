using System.ComponentModel.DataAnnotations;

namespace KronoxFront.DTOs;

public class MainCategoryDto
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Kategorinamn krävs")]
    public string Name { get; set; } = string.Empty;
}
