using System.ComponentModel.DataAnnotations;

namespace KronoxApi.DTOs;

// DTO för att representera en huvudkategori.
public class MainCategoryDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Kategorinamn krävs")]
    public string Name { get; set; } = string.Empty;

    public List<string> AllowedRoles { get; set; } = new();

    public bool IsActive { get; set; } = true;
}