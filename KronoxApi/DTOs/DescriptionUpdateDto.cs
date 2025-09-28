using System.ComponentModel.DataAnnotations;

namespace KronoxApi.DTOs;

// DTO för att uppdatera beskrivnings/alt-text (för en medlemslogotyp).
public class DescriptionUpdateDto
{
    [Required(ErrorMessage = "Beskrivning är obligatorisk.")]
    [StringLength(200, ErrorMessage = "Beskrivning får vara max 200 tecken.")]
    public string Description { get; set; } = string.Empty;
}