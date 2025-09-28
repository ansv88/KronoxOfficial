using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Models;

// Representerar en huvudkategori f�r dokument eller inneh�ll.
public class MainCategory
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Namn p� huvudkategori �r obligatoriskt.")]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    // F�lt f�r rollbaserad �tkomst och arkivering
    public List<string> AllowedRoles { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<Document> Documents { get; set; } = new List<Document>();

    // Hj�lpmetoder f�r rollvalidering
    public bool IsAccessibleByRole(string roleName)
        => AllowedRoles.Contains(roleName, StringComparer.OrdinalIgnoreCase);

    public bool IsAccessibleByRoles(IEnumerable<string> roleNames)
        => AllowedRoles.Any(role => roleNames.Contains(role, StringComparer.OrdinalIgnoreCase));
}