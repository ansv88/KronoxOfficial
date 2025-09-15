namespace KronoxFront.ViewModels;

public class MainCategoryViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<string> AllowedRoles { get; set; } = new();
    public bool IsActive { get; set; } = true;
}