using System.ComponentModel.DataAnnotations;

namespace KronoxFront.DTOs;

// Representerar en huvudkategori för dokument i systemet
public class MainCategoryDto
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Kategorinamn krävs")]
    public string Name { get; set; } = string.Empty;
}