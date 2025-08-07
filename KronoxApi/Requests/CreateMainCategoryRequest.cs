using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Requests;

// Request f�r att skapa en ny huvudkategori med rollbaserad �tkomst
public class CreateMainCategoryRequest
{
    [Required(ErrorMessage = "Kategorinamn kr�vs")]
    [MaxLength(100, ErrorMessage = "Kategorinamnet f�r inte �verstiga 100 tecken")]
    public string Name { get; set; } = string.Empty;
    
    public List<string> AllowedRoles { get; set; } = new();
}