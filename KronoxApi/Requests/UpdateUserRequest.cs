using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Requests;

// Request för uppdatering av en användares profilinformation.
public class UpdateUserRequest
{
    [Required(ErrorMessage = "Förnamn är obligatoriskt.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Efternamn är obligatoriskt.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-postadress är obligatorisk.")]
    [EmailAddress(ErrorMessage = "Ogiltig e-postadress.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Lärosäte är obligatoriskt.")]
    public string Academy { get; set; } = string.Empty;
}