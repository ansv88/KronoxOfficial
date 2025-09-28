using System.ComponentModel.DataAnnotations;

namespace KronoxApi.DTOs;

// DTO för att flytta en medlemslogotyp upp eller ner i listan.
public class LogoMoveDto : IValidatableObject
{
    [Required]
    public int LogoId { get; set; }

    // Endast -1 (upp) eller 1 (ner) är tillåtna
    public int Direction { get; set; }  // -1 = upp, 1 = ner

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Direction != -1 && Direction != 1)
        {
            yield return new ValidationResult(
                "Riktningen måste vara -1 (upp) eller 1 (ner).",
                new[] { nameof(Direction) });
        }
    }
}