using System.ComponentModel.DataAnnotations;

namespace KronoxApi.DTOs;

// DTO för inloggning.
public class LoginDto
{
    [Required(ErrorMessage = "Användarnamn krävs")]
    [StringLength(100, ErrorMessage = "Användarnamnet är för långt.")]
    public required string Username { get; set; }

    [Required(ErrorMessage = "Lösenord krävs")]
    [StringLength(256, ErrorMessage = "Lösenordet är för långt.")]
    public required string Password { get; set; }
}