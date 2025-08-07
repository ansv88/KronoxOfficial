using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Requests;

// Request för att uppdatera en underkategori
public class UpdateSubCategoryRequest
{
    [Required(ErrorMessage = "Kategorinamn krävs")]
    [MaxLength(100, ErrorMessage = "Kategorinamnet får inte överstiga 100 tecken")]
    public string Name { get; set; } = string.Empty;
}