using System.ComponentModel.DataAnnotations;

namespace KronoxFront.ViewModels.Users;

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Nytt l�senord m�ste anges")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "L�senordet m�ste vara minst 6 tecken.")]
    public string NewPassword { get; set; } = "";

    [Required(ErrorMessage = "Bekr�fta l�senordet")]
    [Compare("NewPassword", ErrorMessage = "L�senorden matchar inte.")]
    public string ConfirmPassword { get; set; } = "";
}