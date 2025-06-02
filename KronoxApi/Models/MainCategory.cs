
using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Models;

// Representerar en huvudkategori för dokument eller innehåll.
public class MainCategory
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Namn på huvudkategori är obligatoriskt.")]
    public string Name { get; set; } = string.Empty;
}