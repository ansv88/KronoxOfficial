using System.ComponentModel.DataAnnotations;

namespace KronoxFront.ViewModels;

// ViewModel för hantering av kontaktinformation på kontaktsidan
public class ContactPageInfoViewModel
{
    // Postadressinformation
    public ContactPostalAddressViewModel PostalAddress { get; set; } = new();

    // Lista över kontaktpersoner i systemförvaltargruppen
    public List<ContactPagePersonViewModel> ContactPersons { get; set; } = new();
    // Lista över e-postlistor (endast synliga för medlemmar)
    public List<EmailListViewModel> EmailLists { get; set; } = new();
}

// ViewModel för postadress på kontaktsidan
public class ContactPostalAddressViewModel
{
    [Required(ErrorMessage = "Organisationsnamn är obligatoriskt")]
    [StringLength(100, ErrorMessage = "Organisationsnamn får vara max 100 tecken")]
    public string OrganizationName { get; set; } = "KronoX";

    [Required(ErrorMessage = "Adressrad 1 är obligatorisk")]
    [StringLength(100, ErrorMessage = "Adressrad 1 får vara max 100 tecken")]
    public string AddressLine1 { get; set; } = "Högskolan i Borås";

    [StringLength(100, ErrorMessage = "Adressrad 2 får vara max 100 tecken")]
    public string AddressLine2 { get; set; } = "";

    [Required(ErrorMessage = "Postnummer är obligatoriskt")]
    [StringLength(20, ErrorMessage = "Postnummer får vara max 20 tecken")]
    public string PostalCode { get; set; } = "501 90";

    [Required(ErrorMessage = "Postort är obligatorisk")]
    [StringLength(50, ErrorMessage = "Postort får vara max 50 tecken")]
    public string City { get; set; } = "Borås";

    [StringLength(50, ErrorMessage = "Land får vara max 50 tecken")]
    public string Country { get; set; } = "Sverige";
}

// ViewModel för kontaktperson på kontaktsidan
public class ContactPagePersonViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Namn är obligatoriskt")]
    [StringLength(100, ErrorMessage = "Namn får vara max 100 tecken")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Titel är obligatorisk")]
    [StringLength(100, ErrorMessage = "Titel får vara max 100 tecken")]
    public string Title { get; set; } = "";

    [Required(ErrorMessage = "E-post är obligatorisk")]
    [EmailAddress(ErrorMessage = "Ogiltig e-postadress")]
    [StringLength(100, ErrorMessage = "E-post får vara max 100 tecken")]
    public string Email { get; set; } = "";

    [StringLength(50, ErrorMessage = "Telefon får vara max 50 tecken")]
    public string Phone { get; set; } = "";

    [Range(0, 999, ErrorMessage = "Sorteringsordning måste vara mellan 0 och 999")]
    public int SortOrder { get; set; }

    // Om personen är aktiv/visas på sidan
    public bool IsActive { get; set; } = true;
}

// ViewModel för e-postlista
public class EmailListViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Namn är obligatoriskt")]
    [StringLength(100, ErrorMessage = "Namn får vara max 100 tecken")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Beskrivning är obligatorisk")]
    [StringLength(200, ErrorMessage = "Beskrivning får vara max 200 tecken")]
    public string Description { get; set; } = "";

    [Required(ErrorMessage = "E-postadress är obligatorisk")]
    [EmailAddress(ErrorMessage = "Ogiltig e-postadress")]
    [StringLength(100, ErrorMessage = "E-postadress får vara max 100 tecken")]
    public string EmailAddress { get; set; } = "";

    [Range(0, 999, ErrorMessage = "Sorteringsordning måste vara mellan 0 och 999")]
    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}