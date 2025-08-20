using System.ComponentModel.DataAnnotations;

namespace KronoxFront.ViewModels;

// ViewModel f�r hantering av kontaktinformation p� kontaktsidan
public class ContactPageInfoViewModel
{
    // Postadressinformation
    public ContactPostalAddressViewModel PostalAddress { get; set; } = new();

    // Lista �ver kontaktpersoner i systemf�rvaltargruppen
    public List<ContactPagePersonViewModel> ContactPersons { get; set; } = new();
    // Lista �ver e-postlistor (endast synliga f�r medlemmar)
    public List<EmailListViewModel> EmailLists { get; set; } = new();
}

// ViewModel f�r postadress p� kontaktsidan
public class ContactPostalAddressViewModel
{
    [Required(ErrorMessage = "Organisationsnamn �r obligatoriskt")]
    [StringLength(100, ErrorMessage = "Organisationsnamn f�r vara max 100 tecken")]
    public string OrganizationName { get; set; } = "KronoX";

    [Required(ErrorMessage = "Adressrad 1 �r obligatorisk")]
    [StringLength(100, ErrorMessage = "Adressrad 1 f�r vara max 100 tecken")]
    public string AddressLine1 { get; set; } = "H�gskolan i Bor�s";

    [StringLength(100, ErrorMessage = "Adressrad 2 f�r vara max 100 tecken")]
    public string AddressLine2 { get; set; } = "";

    [Required(ErrorMessage = "Postnummer �r obligatoriskt")]
    [StringLength(20, ErrorMessage = "Postnummer f�r vara max 20 tecken")]
    public string PostalCode { get; set; } = "501 90";

    [Required(ErrorMessage = "Postort �r obligatorisk")]
    [StringLength(50, ErrorMessage = "Postort f�r vara max 50 tecken")]
    public string City { get; set; } = "Bor�s";

    [StringLength(50, ErrorMessage = "Land f�r vara max 50 tecken")]
    public string Country { get; set; } = "Sverige";
}

// ViewModel f�r kontaktperson p� kontaktsidan
public class ContactPagePersonViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Namn �r obligatoriskt")]
    [StringLength(100, ErrorMessage = "Namn f�r vara max 100 tecken")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Titel �r obligatorisk")]
    [StringLength(100, ErrorMessage = "Titel f�r vara max 100 tecken")]
    public string Title { get; set; } = "";

    [Required(ErrorMessage = "E-post �r obligatorisk")]
    [EmailAddress(ErrorMessage = "Ogiltig e-postadress")]
    [StringLength(100, ErrorMessage = "E-post f�r vara max 100 tecken")]
    public string Email { get; set; } = "";

    [StringLength(50, ErrorMessage = "Telefon f�r vara max 50 tecken")]
    public string Phone { get; set; } = "";

    [Range(0, 999, ErrorMessage = "Sorteringsordning m�ste vara mellan 0 och 999")]
    public int SortOrder { get; set; }

    // Om personen �r aktiv/visas p� sidan
    public bool IsActive { get; set; } = true;
}

// ViewModel f�r e-postlista
public class EmailListViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Namn �r obligatoriskt")]
    [StringLength(100, ErrorMessage = "Namn f�r vara max 100 tecken")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Beskrivning �r obligatorisk")]
    [StringLength(200, ErrorMessage = "Beskrivning f�r vara max 200 tecken")]
    public string Description { get; set; } = "";

    [Required(ErrorMessage = "E-postadress �r obligatorisk")]
    [EmailAddress(ErrorMessage = "Ogiltig e-postadress")]
    [StringLength(100, ErrorMessage = "E-postadress f�r vara max 100 tecken")]
    public string EmailAddress { get; set; } = "";

    [Range(0, 999, ErrorMessage = "Sorteringsordning m�ste vara mellan 0 och 999")]
    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}