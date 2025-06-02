using System.ComponentModel.DataAnnotations;

namespace KronoxFront.ViewModels;

// Motsvarar PageContentDto i API-projektet
public class PageContentViewModel
{
    public string PageKey { get; set; } = "";
    public string Title { get; set; } = "";
    public string HtmlContent { get; set; } = "";
    public string Metadata { get; set; } = "{}";     // Fält för separerad metadata
    public DateTime LastModified { get; set; }
    public List<PageImageViewModel> Images { get; set; } = new();
}

// Motsvarar PageImageDto
public class PageImageViewModel
{
    public int Id { get; set; }
    public string Url { get; set; } = "";
    public string AltText { get; set; } = "";
}

// Motsvarar MemberLogoDto
public class MemberLogoViewModel
{
    public int Id { get; set; }
    public string Url { get; set; } = "";
    public string AltText { get; set; } = "";
    public int SortOrd { get; set; }
    public string LinkUrl { get; set; } = "";
}

// För att skapa nya logotyper
public class LogoCreationModel
{
    public string AltText { get; set; } = string.Empty;
}

// Request-modeller för API-anrop
public class DescriptionUpdateRequest
{
    public string Description { get; set; } = string.Empty;
}

public class LinkUrlUpdateRequest
{
    public string LinkUrl { get; set; } = string.Empty;
}

public class LinkUrlUpdateViewModel
{
    public string LinkUrl { get; set; } = "";
}

public class LogoMoveRequest
{
    public int LogoId { get; set; }
    public int Direction { get; set; }  // -1 = upp, 1 = ner
}

// Intro-sektion för startsidan
public class IntroSectionViewModel
{
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public string ImageAltText { get; set; } = "";
    public bool HasImage { get; set; } = false;
}

// Feature-sektion för startsidan
public class HomeFeatureSectionViewModel
{
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public string ImageAltText { get; set; } = "";
    public bool HasImage { get; set; } = true;
}