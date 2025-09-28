using System.ComponentModel.DataAnnotations;

namespace KronoxFront.ViewModels.Users;

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Nytt lösenord måste anges")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Lösenordet måste vara minst 6 tecken.")]
    public string NewPassword { get; set; } = "";

    [Required(ErrorMessage = "Bekräfta lösenordet")]
    [Compare("NewPassword", ErrorMessage = "Lösenorden matchar inte.")]
    public string ConfirmPassword { get; set; } = "";
}