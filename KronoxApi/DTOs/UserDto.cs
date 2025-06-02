
namespace KronoxApi.DTOs;

// DTO för att representera användardata som returneras till klienten efter autentisering. Innehåller grundläggande användarinformation och roller.
public class UserDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}