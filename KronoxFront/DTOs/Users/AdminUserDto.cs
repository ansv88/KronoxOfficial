using System.ComponentModel.DataAnnotations;

namespace KronoxFront.DTOs.Users;

public class AdminUserDto
{
    [StringLength(100)]
    public string UserName { get; set; } = "";

    [StringLength(100)]
    public string FirstName { get; set; } = "";

    [StringLength(100)]
    public string LastName { get; set; } = "";

    [EmailAddress]
    [StringLength(200)]
    public string Email { get; set; } = "";

    [StringLength(100)]
    public string Academy { get; set; } = "";

    public List<string> Roles { get; set; } = new();

    public DateTime? CreatedDate { get; set; }
}