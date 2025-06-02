using System.ComponentModel.DataAnnotations;

namespace KronoxFront.Requests;

public class UserRegisterRequest
{
    [Required(ErrorMessage = "Anv�ndarnamn kr�vs")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "V�nligen ange en e-postadress.")]
    [EmailAddress(ErrorMessage = "Ange en giltig e-postadress.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "V�nligen ange ett l�senord")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "V�nligen bekr�fta ditt l�senord.")]
    [Compare("Password", ErrorMessage = "L�senorden matchar inte.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "V�nligen ange ditt f�rnamn.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "V�nligen ange ditt efternamn.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "V�nligen ange ditt l�ros�te.")]
    public string Academy { get; set; } = string.Empty;
}
