namespace KronoxFront.Services;

public class NavigationEvents
{
    public event Action? NavigationUpdated;
    public void NotifyUpdated() => NavigationUpdated?.Invoke();
}