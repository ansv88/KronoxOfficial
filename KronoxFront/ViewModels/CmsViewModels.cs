using System.ComponentModel.DataAnnotations;

namespace KronoxFront.ViewModels;

// Innehåller ViewModels för CMS-data såsom sidor, bilder, logotyper.
// OBS: IntroSectionViewModel har flyttats till separat fil.

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
    public string PageKey { get; set; } = "";
    public bool IsActive { get; set; }
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

// För kontaktpersoner i privat information
public class ContactPersonViewModel
{
    [Required(ErrorMessage = "Namn är obligatoriskt.")]
    [StringLength(100, ErrorMessage = "Namn får vara max 100 tecken.")]
    public string Name { get; set; } = "";

    [StringLength(256, ErrorMessage = "E-post får vara max 256 tecken.")]
    [EmailAddress(ErrorMessage = "Ogiltig e-postadress.")]
    public string Email { get; set; } = "";

    [StringLength(30, ErrorMessage = "Telefon får vara max 30 tecken.")]
    public string Phone { get; set; } = "";

    [StringLength(100, ErrorMessage = "Organisation får vara max 100 tecken.")]
    public string Organization { get; set; } = "";
}