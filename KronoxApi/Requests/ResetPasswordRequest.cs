using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Requests;

// Request för återställning av lösenord för en användare.
public class ResetPasswordRequest
{
    [Required(ErrorMessage = "Användarnamn måste anges")]
    public string UserName { get; set; } = "";

    [Required(ErrorMessage = "Nytt lösenord måste anges")]
    public string NewPassword { get; set; } = "";
}