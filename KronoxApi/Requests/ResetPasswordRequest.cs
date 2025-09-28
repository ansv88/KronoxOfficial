using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Requests;

// Request f�r �terst�llning av l�senord f�r en anv�ndare.
public class ResetPasswordRequest
{
    [Required(ErrorMessage = "Anv�ndarnamn m�ste anges")]
    [StringLength(100, ErrorMessage = "Anv�ndarnamnet �r f�r l�ngt.")]
    public string UserName { get; set; } = "";

    [Required(ErrorMessage = "Nytt l�senord m�ste anges")]
    [StringLength(256, ErrorMessage = "L�senordet �r f�r l�ngt.")]
    public string NewPassword { get; set; } = "";
}