namespace KronoxFront.ViewModels;

public class SectionConfigItem
{
    public SectionType Type { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; }
    public string? CustomTitle { get; set; }
}