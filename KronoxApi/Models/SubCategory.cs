using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Models;

// Underkategori f�r dokument (namn).
public class SubCategory 
{
    public int Id { get; set; }
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}