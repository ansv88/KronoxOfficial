using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Models;

// Inskickat utvecklingsförslag (kontaktuppgifter, behov, förväntad nytta, status).
public class DevelopmentSuggestion
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Organization { get; set; } = "";

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = "";

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = "";

    [Required]
    [StringLength(2000)]
    public string Requirement { get; set; } = "";

    [Required]
    [StringLength(2000)]
    public string ExpectedBenefit { get; set; } = "";

    [StringLength(2000)]
    public string AdditionalInfo { get; set; } = "";

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public bool IsProcessed { get; set; } = false;

    [StringLength(100)] // matchar EF
    public string? ProcessedBy { get; set; }

    public DateTime? ProcessedAt { get; set; }
}