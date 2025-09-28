using System.ComponentModel.DataAnnotations;

namespace KronoxFront.Requests.Admin;

public class ResetPasswordRequest
{
    [Required]
    public string UserName { get; set; } = "";

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string NewPassword { get; set; } = "";
}