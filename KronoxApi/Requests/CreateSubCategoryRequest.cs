using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Requests;

// Request för att skapa en ny underkategori
public class CreateSubCategoryRequest
{
    [Required(ErrorMessage = "Kategorinamn krävs")]
    [MaxLength(50, ErrorMessage = "Kategorinamnet får inte överstiga 50 tecken")]
    public string Name { get; set; } = string.Empty;
}