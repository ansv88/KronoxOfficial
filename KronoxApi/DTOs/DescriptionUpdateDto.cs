using System.ComponentModel.DataAnnotations;

namespace KronoxApi.DTOs;

// DTO för att uppdatera beskrivnings/alt-text (för en medlemslogotyp).
public class DescriptionUpdateDto
{
    [Required(ErrorMessage = "Beskrivning är obligatorisk.")]
    public string Description { get; set; } = string.Empty;
}