using System.ComponentModel.DataAnnotations;

namespace KronoxFront.ViewModels;

public class ContactFormViewModel
{
    [Required(ErrorMessage = "Namn �r obligatoriskt")]
    [StringLength(100, ErrorMessage = "Namn f�r vara max 100 tecken")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "E-post �r obligatoriskt")]
    [EmailAddress(ErrorMessage = "Ogiltig e-postadress")]
    [StringLength(100, ErrorMessage = "E-post f�r vara max 100 tecken")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "�mne �r obligatoriskt")]
    [StringLength(200, ErrorMessage = "�mne f�r vara max 200 tecken")]
    public string Subject { get; set; } = "";

    [Required(ErrorMessage = "Meddelande �r obligatoriskt")]
    [StringLength(2000, ErrorMessage = "Meddelande f�r vara max 2000 tecken")]
    public string Message { get; set; } = "";
}