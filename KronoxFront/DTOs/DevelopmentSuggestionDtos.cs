using System.ComponentModel.DataAnnotations;

namespace KronoxFront.DTOs;

public class DevelopmentSuggestionDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Lärosäte/organisation är obligatoriskt")]
    [StringLength(200, ErrorMessage = "Lärosäte/organisation får vara max 200 tecken")]
    public string Organization { get; set; } = "";

    [Required(ErrorMessage = "Namn är obligatoriskt")]
    [StringLength(100, ErrorMessage = "Namn får vara max 100 tecken")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "E-postadress är obligatoriskt")]
    [EmailAddress(ErrorMessage = "Ogiltig e-postadress")]
    [StringLength(100, ErrorMessage = "E-post får vara max 100 tecken")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Behovet är obligatoriskt")]
    [StringLength(2000, ErrorMessage = "Behovet får vara max 2000 tecken")]
    [MinLength(10, ErrorMessage = "Behovet måste vara minst 10 tecken")]
    public string Requirement { get; set; } = "";

    [Required(ErrorMessage = "Förväntad effekt/nytta är obligatoriskt")]
    [StringLength(2000, ErrorMessage = "Förväntad effekt/nytta får vara max 2000 tecken")]
    [MinLength(10, ErrorMessage = "Förväntad effekt/nytta måste vara minst 10 tecken")]
    public string ExpectedBenefit { get; set; } = "";

    [StringLength(2000, ErrorMessage = "Ytterligare info får vara max 2000 tecken")]
    public string AdditionalInfo { get; set; } = "";

    public DateTime SubmittedAt { get; set; }
    public bool IsProcessed { get; set; }
    public string? ProcessedBy { get; set; }
    public DateTime? ProcessedAt { get; set; }
}

public class CreateDevelopmentSuggestionDto
{
    [Required(ErrorMessage = "Lärosäte/organisation är obligatoriskt")]
    [StringLength(200, ErrorMessage = "Lärosäte/organisation får vara max 200 tecken")]
    public string Organization { get; set; } = "";

    [Required(ErrorMessage = "Namn är obligatoriskt")]
    [StringLength(100, ErrorMessage = "Namn får vara max 100 tecken")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "E-postadress är obligatoriskt")]
    [EmailAddress(ErrorMessage = "Ogiltig e-postadress")]
    [StringLength(100, ErrorMessage = "E-post får vara max 100 tecken")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Behovet är obligatoriskt")]
    [StringLength(2000, ErrorMessage = "Behovet får vara max 2000 tecken")]
    [MinLength(10, ErrorMessage = "Behovet måste vara minst 10 tecken")]
    public string Requirement { get; set; } = "";

    [Required(ErrorMessage = "Förväntad effekt/nytta är obligatoriskt")]
    [StringLength(2000, ErrorMessage = "Förväntad effekt/nytta får vara max 2000 tecken")]
    [MinLength(10, ErrorMessage = "Förväntad effekt/nytta måste vara minst 10 tecken")]
    public string ExpectedBenefit { get; set; } = "";

    [StringLength(2000, ErrorMessage = "Ytterligare info får vara max 2000 tecken")]
    public string AdditionalInfo { get; set; } = "";
}