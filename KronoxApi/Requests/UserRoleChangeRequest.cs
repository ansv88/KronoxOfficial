using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Requests;

// Request för att ändra roll för en användare
public class UserRoleChangeRequest
{
    [Required(ErrorMessage = "Användarnamn krävs")]
    [StringLength(100, ErrorMessage = "Användarnamnet är för långt.")]
    public required string UserName { get; set; }

    [Required(ErrorMessage = "Rollnamn krävs")]
    [StringLength(100, ErrorMessage = "Rollnamnet är för långt.")]
    public required string RoleName { get; set; }
}