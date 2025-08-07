using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Requests;

// Request för att uppdatera en huvudkategori
public class UpdateMainCategoryRequest
{
    [Required(ErrorMessage = "Kategorinamn krävs")]
    [MaxLength(100, ErrorMessage = "Kategorinamnet får inte överstiga 100 tecken")]
    public string Name { get; set; } = string.Empty;
    
    public List<string> AllowedRoles { get; set; } = new();
}