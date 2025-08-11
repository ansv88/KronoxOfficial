namespace KronoxFront.ViewModels;

// ViewModel för navigeringsknappar som visas under intro-sektionen på olika sidor.
public class NavigationButtonViewModel
{
    public string Text { get; set; } = "";
    public string Url { get; set; } = "";
    public string IconClass { get; set; } = "fa-solid fa-arrow-right";
    public int SortOrder { get; set; }
}