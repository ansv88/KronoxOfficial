using System.ComponentModel.DataAnnotations;

namespace KronoxApi.DTOs;

// Data Transfer Object för underkategori i dokumenthanteringssystemet.
// Används för att överföra underkategoriinformation mellan API och klienter.
public class SubCategoryDto
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Namn på underkategori krävs")]
    [MaxLength(100, ErrorMessage = "Namnet får inte överstiga 100 tecken")]
    public string Name { get; set; } = string.Empty;
}