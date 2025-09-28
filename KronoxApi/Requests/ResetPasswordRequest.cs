using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Requests;

// Request för återställning av lösenord för en användare.
public class ResetPasswordRequest
{
    [Required(ErrorMessage = "Användarnamn måste anges")]
    [StringLength(100, ErrorMessage = "Användarnamnet är för långt.")]
    public string UserName { get; set; } = "";

    [Required(ErrorMessage = "Nytt lösenord måste anges")]
    [StringLength(256, ErrorMessage = "Lösenordet är för långt.")]
    public string NewPassword { get; set; } = "";
}