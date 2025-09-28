using System.ComponentModel.DataAnnotations;

namespace KronoxFront.Requests.Admin;

public class UserRoleChangeRequest
{
    [Required(ErrorMessage = "Användarnamn krävs")]
    public string UserName { get; set; } = "";

    [Required(ErrorMessage = "Roll krävs")]
    public string RoleName { get; set; } = "";
}