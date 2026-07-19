using System.ComponentModel.DataAnnotations;

namespace KronoxFront.DTOs;

// DTO för att representera en huvudkategori.
public class MainCategoryDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Kategorinamn krävs")]
    [StringLength(50, ErrorMessage = "Kategorinamnet får vara max 50 tecken.")]
    public string Name { get; set; } = string.Empty;

    public List<string> AllowedRoles { get; set; } = new();
    public bool IsActive { get; set; } = true;
}