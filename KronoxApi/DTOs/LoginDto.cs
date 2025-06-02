using System.ComponentModel.DataAnnotations;

namespace KronoxApi.DTOs;

// DTO för inloggning.
public class LoginDto
{
    [Required(ErrorMessage = "Användarnamn krävs")]
    public required string Username { get; set; }

    [Required(ErrorMessage = "Lösenord krävs")]
    public required string Password { get; set; }
}