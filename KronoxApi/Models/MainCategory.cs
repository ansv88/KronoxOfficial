
using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Models;

// Representerar en huvudkategori f�r dokument eller inneh�ll.
public class MainCategory
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Namn p� huvudkategori �r obligatoriskt.")]
    public string Name { get; set; } = string.Empty;
}