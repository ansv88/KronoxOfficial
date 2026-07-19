using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Requests;

// Request för att uppdatera en underkategori
public class UpdateSubCategoryRequest
{
    [Required(ErrorMessage = "Kategorinamn krävs")]
    [MaxLength(50, ErrorMessage = "Kategorinamnet får inte överstiga 50 tecken")]
    public string Name { get; set; } = string.Empty;
}