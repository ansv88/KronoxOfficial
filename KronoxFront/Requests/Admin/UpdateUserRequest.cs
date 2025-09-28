using System.ComponentModel.DataAnnotations;

namespace KronoxFront.Requests.Admin;

public class UpdateUserRequest
{
    [Required(ErrorMessage = "F�rnamn kr�vs")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Efternamn kr�vs")]
    public string LastName { get; set; } = "";

    [Required(ErrorMessage = "E-post kr�vs")]
    [EmailAddress(ErrorMessage = "Ogiltig e-postadress")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "L�ros�te kr�vs")]
    public string Academy { get; set; } = "";
}