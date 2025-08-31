using System.ComponentModel.DataAnnotations;

namespace KronoxApi.DTOs;

public class DevelopmentSuggestionDto
{
    [Required(ErrorMessage = "Lärosäte/organisation är obligatoriskt")]
    [StringLength(200, ErrorMessage = "Lärosäte/organisation får vara max 200 tecken")]
    public string Organization { get; set; } = string.Empty;

    [Required(ErrorMessage = "Namn är obligatoriskt")]
    [StringLength(100, ErrorMessage = "Namn får vara max 100 tecken")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-postadress är obligatoriskt")]
    [EmailAddress(ErrorMessage = "Ogiltig e-postadress")]
    [StringLength(100, ErrorMessage = "E-post får vara max 100 tecken")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Behovet är obligatoriskt")]
    [StringLength(2000, ErrorMessage = "Behovet får vara max 2000 tecken")]
    [MinLength(10, ErrorMessage = "Behovet måste vara minst 10 tecken")]
    public string Requirement { get; set; } = string.Empty;

    [Required(ErrorMessage = "Förväntad effekt/nytta är obligatoriskt")]
    [StringLength(2000, ErrorMessage = "Förväntad effekt/nytta får vara max 2000 tecken")]
    [MinLength(10, ErrorMessage = "Förväntad effekt/nytta måste vara minst 10 tecken")]
    public string ExpectedBenefit { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Ytterligare info får vara max 2000 tecken")]
    public string AdditionalInfo { get; set; } = string.Empty;
}