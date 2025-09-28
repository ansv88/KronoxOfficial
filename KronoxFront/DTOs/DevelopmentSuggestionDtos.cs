using System.ComponentModel.DataAnnotations;

namespace KronoxFront.DTOs;

public class DevelopmentSuggestionDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "L�ros�te/organisation �r obligatoriskt")]
    [StringLength(200, ErrorMessage = "L�ros�te/organisation f�r vara max 200 tecken")]
    public string Organization { get; set; } = "";

    [Required(ErrorMessage = "Namn �r obligatoriskt")]
    [StringLength(100, ErrorMessage = "Namn f�r vara max 100 tecken")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "E-postadress �r obligatoriskt")]
    [EmailAddress(ErrorMessage = "Ogiltig e-postadress")]
    [StringLength(100, ErrorMessage = "E-post f�r vara max 100 tecken")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Behovet �r obligatoriskt")]
    [StringLength(2000, ErrorMessage = "Behovet f�r vara max 2000 tecken")]
    [MinLength(10, ErrorMessage = "Behovet m�ste vara minst 10 tecken")]
    public string Requirement { get; set; } = "";

    [Required(ErrorMessage = "F�rv�ntad effekt/nytta �r obligatoriskt")]
    [StringLength(2000, ErrorMessage = "F�rv�ntad effekt/nytta f�r vara max 2000 tecken")]
    [MinLength(10, ErrorMessage = "F�rv�ntad effekt/nytta m�ste vara minst 10 tecken")]
    public string ExpectedBenefit { get; set; } = "";

    [StringLength(2000, ErrorMessage = "Ytterligare info f�r vara max 2000 tecken")]
    public string AdditionalInfo { get; set; } = "";

    public DateTime SubmittedAt { get; set; }
    public bool IsProcessed { get; set; }
    public string? ProcessedBy { get; set; }
    public DateTime? ProcessedAt { get; set; }
}

public class CreateDevelopmentSuggestionDto
{
    [Required(ErrorMessage = "L�ros�te/organisation �r obligatoriskt")]
    [StringLength(200, ErrorMessage = "L�ros�te/organisation f�r vara max 200 tecken")]
    public string Organization { get; set; } = "";

    [Required(ErrorMessage = "Namn �r obligatoriskt")]
    [StringLength(100, ErrorMessage = "Namn f�r vara max 100 tecken")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "E-postadress �r obligatoriskt")]
    [EmailAddress(ErrorMessage = "Ogiltig e-postadress")]
    [StringLength(100, ErrorMessage = "E-post f�r vara max 100 tecken")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Behovet �r obligatoriskt")]
    [StringLength(2000, ErrorMessage = "Behovet f�r vara max 2000 tecken")]
    [MinLength(10, ErrorMessage = "Behovet m�ste vara minst 10 tecken")]
    public string Requirement { get; set; } = "";

    [Required(ErrorMessage = "F�rv�ntad effekt/nytta �r obligatoriskt")]
    [StringLength(2000, ErrorMessage = "F�rv�ntad effekt/nytta f�r vara max 2000 tecken")]
    [MinLength(10, ErrorMessage = "F�rv�ntad effekt/nytta m�ste vara minst 10 tecken")]
    public string ExpectedBenefit { get; set; } = "";

    [StringLength(2000, ErrorMessage = "Ytterligare info f�r vara max 2000 tecken")]
    public string AdditionalInfo { get; set; } = "";
}