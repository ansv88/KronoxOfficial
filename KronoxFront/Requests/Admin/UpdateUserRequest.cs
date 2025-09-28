using System.ComponentModel.DataAnnotations;

namespace KronoxFront.Requests.Admin;

public class UpdateUserRequest
{
    [Required(ErrorMessage = "Förnamn krävs")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Efternamn krävs")]
    public string LastName { get; set; } = "";

    [Required(ErrorMessage = "E-post krävs")]
    [EmailAddress(ErrorMessage = "Ogiltig e-postadress")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Lärosäte krävs")]
    public string Academy { get; set; } = "";
}