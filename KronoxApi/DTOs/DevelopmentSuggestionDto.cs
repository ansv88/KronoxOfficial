using System.ComponentModel.DataAnnotations;

namespace KronoxApi.DTOs;

public class DevelopmentSuggestionDto
{
    [Required(ErrorMessage = "L�ros�te/organisation �r obligatoriskt")]
    [StringLength(200, ErrorMessage = "L�ros�te/organisation f�r vara max 200 tecken")]
    public string Organization { get; set; } = string.Empty;

    [Required(ErrorMessage = "Namn �r obligatoriskt")]
    [StringLength(100, ErrorMessage = "Namn f�r vara max 100 tecken")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-postadress �r obligatoriskt")]
    [EmailAddress(ErrorMessage = "Ogiltig e-postadress")]
    [StringLength(100, ErrorMessage = "E-post f�r vara max 100 tecken")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Behovet �r obligatoriskt")]
    [StringLength(2000, ErrorMessage = "Behovet f�r vara max 2000 tecken")]
    [MinLength(10, ErrorMessage = "Behovet m�ste vara minst 10 tecken")]
    public string Requirement { get; set; } = string.Empty;

    [Required(ErrorMessage = "F�rv�ntad effekt/nytta �r obligatoriskt")]
    [StringLength(2000, ErrorMessage = "F�rv�ntad effekt/nytta f�r vara max 2000 tecken")]
    [MinLength(10, ErrorMessage = "F�rv�ntad effekt/nytta m�ste vara minst 10 tecken")]
    public string ExpectedBenefit { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Ytterligare info f�r vara max 2000 tecken")]
    public string AdditionalInfo { get; set; } = string.Empty;
}