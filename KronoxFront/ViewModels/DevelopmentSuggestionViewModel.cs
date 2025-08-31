namespace KronoxFront.ViewModels;

public class DevelopmentSuggestionViewModel
{
    public int Id { get; set; }
    public string Organization { get; set; } = "";
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Requirement { get; set; } = "";
    public string ExpectedBenefit { get; set; } = "";
    public string AdditionalInfo { get; set; } = "";
    public DateTime SubmittedAt { get; set; }
    public bool IsProcessed { get; set; }
    public string? ProcessedBy { get; set; }
    public DateTime? ProcessedAt { get; set; }
}