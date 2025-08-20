using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Models;

// Entitet för postadress
public class PostalAddress
{
    public int Id { get; set; }

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

    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}

// Entitet för kontaktperson
public class ContactPerson
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

    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}

// Entitet för e-postlistor (endast synliga för medlemmar)
public class EmailList
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

    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}