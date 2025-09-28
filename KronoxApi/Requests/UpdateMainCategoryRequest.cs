using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Requests;

// Request f�r att uppdatera en huvudkategori
public class UpdateMainCategoryRequest
{
    [Required(ErrorMessage = "Kategorinamn kr�vs")]
    [MaxLength(255, ErrorMessage = "Kategorinamnet f�r inte �verstiga 255 tecken")]
    public string Name { get; set; } = string.Empty;

    public List<string> AllowedRoles { get; set; } = new();
}