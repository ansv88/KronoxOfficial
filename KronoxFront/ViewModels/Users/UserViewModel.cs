namespace KronoxFront.ViewModels.Users;

public class UserViewModel
{
    public string UserName { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Academy { get; set; } = "";
    public List<string>? Roles { get; set; }
    public DateTime? CreatedDate { get; set; }
}