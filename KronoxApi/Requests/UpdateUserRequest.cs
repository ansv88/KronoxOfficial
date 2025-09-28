using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Requests;

// Request för uppdatering av en användares profilinformation.
public class UpdateUserRequest
{
    [Required(ErrorMessage = "Förnamn är obligatoriskt.")]
    [StringLength(100, ErrorMessage = "Förnamn får vara max 100 tecken.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Efternamn är obligatoriskt.")]
    [StringLength(100, ErrorMessage = "Efternamn får vara max 100 tecken.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-postadress är obligatorisk.")]
    [EmailAddress(ErrorMessage = "Ogiltig e-postadress.")]
    [StringLength(100, ErrorMessage = "E-post får vara max 100 tecken.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Lärosäte är obligatoriskt.")]
    [StringLength(100, ErrorMessage = "Lärosäte får vara max 100 tecken.")]
    public string Academy { get; set; } = string.Empty;
}