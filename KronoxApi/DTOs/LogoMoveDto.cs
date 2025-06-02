using System.ComponentModel.DataAnnotations;

namespace KronoxApi.DTOs;

// DTO för att flytta en medlemslogotyp upp eller ner i listan.
public class LogoMoveDto
{
    [Required]
    public int LogoId { get; set; }
    [Required]
    [Range(-1, 1)]
    public int Direction { get; set; }  // -1 = upp, 1 = ner
}