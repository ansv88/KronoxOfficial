namespace KronoxFront.DTOs.Users;

public class AdminUserDto
{
    public string UserName { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Academy { get; set; } = "";
    public List<string> Roles { get; set; } = new();
    public DateTime? CreatedDate { get; set; }
}