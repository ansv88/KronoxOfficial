using System.ComponentModel.DataAnnotations;

namespace KronoxFront.ViewModels;

public class ContactFormViewModel
{
    [Required(ErrorMessage = "Namn är obligatoriskt")]
    [StringLength(100, ErrorMessage = "Namn får vara max 100 tecken")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "E-post är obligatoriskt")]
    [EmailAddress(ErrorMessage = "Ogiltig e-postadress")]
    [StringLength(100, ErrorMessage = "E-post får vara max 100 tecken")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Ämne är obligatoriskt")]
    [StringLength(200, ErrorMessage = "Ämne får vara max 200 tecken")]
    public string Subject { get; set; } = "";

    [Required(ErrorMessage = "Meddelande är obligatoriskt")]
    [StringLength(2000, ErrorMessage = "Meddelande får vara max 2000 tecken")]
    public string Message { get; set; } = "";
}