using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Requests;

// Request f�r att �ndra roll f�r en anv�ndare
public class UserRoleChangeRequest
{
    [Required(ErrorMessage = "Anv�ndarnamn kr�vs")]
    [StringLength(100, ErrorMessage = "Anv�ndarnamnet �r f�r l�ngt.")]
    public required string UserName { get; set; }

    [Required(ErrorMessage = "Rollnamn kr�vs")]
    [StringLength(100, ErrorMessage = "Rollnamnet �r f�r l�ngt.")]
    public required string RoleName { get; set; }
}