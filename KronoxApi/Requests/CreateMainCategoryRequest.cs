using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Requests;

// Request för att skapa en ny huvudkategori med rollbaserad åtkomst
public class CreateMainCategoryRequest
{
    [Required(ErrorMessage = "Kategorinamn krävs")]
    [MaxLength(100, ErrorMessage = "Kategorinamnet får inte överstiga 100 tecken")]
    public string Name { get; set; } = string.Empty;
    
    public List<string> AllowedRoles { get; set; } = new();
}