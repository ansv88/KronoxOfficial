using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Models;

public class DevelopmentSuggestion
{
    public int Id { get; set; }
    public string Organization { get; set; } = "";
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Requirement { get; set; } = "";
    public string ExpectedBenefit { get; set; } = "";
    public string AdditionalInfo { get; set; } = "";
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public bool IsProcessed { get; set; } = false;
    public string? ProcessedBy { get; set; }
    public DateTime? ProcessedAt { get; set; }
}