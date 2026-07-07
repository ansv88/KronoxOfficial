using KronoxFront.ViewModels;

namespace KronoxFront.Extensions;

// Hjälpmetoder för att hämta den aktiva bannerbilden för en sida.
// Källa är PageImageViewModel.IsActive (en aktiv bild per sida).
public static class PageImageExtensions
{
    public static PageImageViewModel? GetActiveBanner(this PageContentViewModel? page)
        => page?.Images?.FirstOrDefault(i => i.IsActive);

    public static string GetBannerUrl(this PageContentViewModel? page)
        => page.GetActiveBanner()?.Url ?? "";

    public static string GetBannerAlt(this PageContentViewModel? page)
        => page.GetActiveBanner()?.AltText ?? "";
}