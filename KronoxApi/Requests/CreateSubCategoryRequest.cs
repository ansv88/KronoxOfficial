using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Requests;

// Request f�r att skapa en ny underkategori
public class CreateSubCategoryRequest
{
    [Required(ErrorMessage = "Kategorinamn kr�vs")]
    [MaxLength(100, ErrorMessage = "Kategorinamnet f�r inte �verstiga 100 tecken")]
    public string Name { get; set; } = string.Empty;
}