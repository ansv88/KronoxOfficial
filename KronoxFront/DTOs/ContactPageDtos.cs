using System.ComponentModel.DataAnnotations;

namespace KronoxFront.DTOs;

// DTO för kontaktinformation på Kontakta oss-sidan
public class ContactPageInfoDto
{
    public ContactPostalAddressDto PostalAddress { get; set; } = new();
    public List<ContactPagePersonDto> ContactPersons { get; set; } = new();
    public List<EmailListDto> EmailLists { get; set; } = new();
}

// DTO för postadress på kontaktsidan
public class ContactPostalAddressDto
{
    [Required]
    [StringLength(100)]
    public string OrganizationName { get; set; } = "";

    [Required]
    [StringLength(100)]
    public string AddressLine1 { get; set; } = "";

    [StringLength(100)]
    public string AddressLine2 { get; set; } = "";

    [Required]
    [StringLength(20)]
    public string PostalCode { get; set; } = "";

    [Required]
    [StringLength(50)]
    public string City { get; set; } = "";

    [StringLength(50)]
    public string Country { get; set; } = "";
}

// DTO för kontaktperson på kontaktsidan
public class ContactPagePersonDto
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = "";

    [Required]
    [StringLength(100)]
    public string Title { get; set; } = "";

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = "";

    [StringLength(50)]
    public string Phone { get; set; } = "";

    [Range(0, 999)]
    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

// DTO för att skapa/uppdatera kontaktperson på kontaktsidan
public class UpsertContactPagePersonDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = "";

    [Required]
    [StringLength(100)]
    public string Title { get; set; } = "";

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = "";

    [StringLength(50)]
    public string Phone { get; set; } = "";

    [Range(0, 999)]
    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

// DTO för e-postlista
public class EmailListDto
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = "";

    [Required]
    [StringLength(200)]
    public string Description { get; set; } = "";

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string EmailAddress { get; set; } = "";

    [Range(0, 999)]
    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

// DTO för att skapa/uppdatera e-postlista
public class UpsertEmailListDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = "";

    [Required]
    [StringLength(200)]
    public string Description { get; set; } = "";

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string EmailAddress { get; set; } = "";

    [Range(0, 999)]
    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}