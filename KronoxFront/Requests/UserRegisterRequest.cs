using System.ComponentModel.DataAnnotations;

namespace KronoxFront.Requests;

// Request för att registrera en ny användare

public class UserRegisterRequest
{
    [Required(ErrorMessage = "Användarnamn krävs")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vänligen ange en e-postadress.")]
    [EmailAddress(ErrorMessage = "Ange en giltig e-postadress.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vänligen ange ett lösenord")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vänligen bekräfta ditt lösenord.")]
    [Compare("Password", ErrorMessage = "Lösenorden matchar inte.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vänligen ange ditt förnamn.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vänligen ange ditt efternamn.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vänligen ange ditt lärosäte.")]
    public string Academy { get; set; } = string.Empty;

    // Fylls i programmatiskt före submit – ingen validering i formuläret
    public string? RecaptchaToken { get; set; }
}