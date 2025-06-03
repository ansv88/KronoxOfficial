using System.ComponentModel.DataAnnotations;

namespace KronoxFront.DTOs;

// DTO f�r anv�ndardata som utbyts mellan klient och server
public class UserDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}