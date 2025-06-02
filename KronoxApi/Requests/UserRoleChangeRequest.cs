using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Requests;

// Request för att ändra roll för en användare
public class UserRoleChangeRequest
{
    [Required(ErrorMessage = "Användarnamn krävs")]
    public required string UserName { get; set; }

    [Required(ErrorMessage = "Rollnamn krävs")]
    public required string RoleName { get; set; }
}