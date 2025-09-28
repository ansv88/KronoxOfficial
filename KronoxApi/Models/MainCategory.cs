using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Models;

// Representerar en huvudkategori för dokument eller innehåll.
public class MainCategory
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Namn på huvudkategori är obligatoriskt.")]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    // Fält för rollbaserad åtkomst och arkivering
    public List<string> AllowedRoles { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<Document> Documents { get; set; } = new List<Document>();

    // Hjälpmetoder för rollvalidering
    public bool IsAccessibleByRole(string roleName)
        => AllowedRoles.Contains(roleName, StringComparer.OrdinalIgnoreCase);

    public bool IsAccessibleByRoles(IEnumerable<string> roleNames)
        => AllowedRoles.Any(role => roleNames.Contains(role, StringComparer.OrdinalIgnoreCase));
}