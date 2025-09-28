using System.ComponentModel.DataAnnotations;

namespace KronoxFront.Requests.Admin;

public class UserRoleChangeRequest
{
    [Required(ErrorMessage = "Anv�ndarnamn kr�vs")]
    public string UserName { get; set; } = "";

    [Required(ErrorMessage = "Roll kr�vs")]
    public string RoleName { get; set; } = "";
}