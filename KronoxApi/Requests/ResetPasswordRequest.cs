using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Requests;

// Request f�r �terst�llning av l�senord f�r en anv�ndare.
public class ResetPasswordRequest
{
    [Required(ErrorMessage = "Anv�ndarnamn m�ste anges")]
    public string UserName { get; set; } = "";

    [Required(ErrorMessage = "Nytt l�senord m�ste anges")]
    public string NewPassword { get; set; } = "";
}