using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Requests;

// Request f�r att �ndra roll f�r en anv�ndare
public class UserRoleChangeRequest
{
    [Required(ErrorMessage = "Anv�ndarnamn kr�vs")]
    public required string UserName { get; set; }

    [Required(ErrorMessage = "Rollnamn kr�vs")]
    public required string RoleName { get; set; }
}