using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Models;

// Anpassad användarmodell som utökar ASP.NET Identity's IdentityUser.
// Notera att egenskaper som Id, UserName, Email etc. ärvs från IdentityUser.
public class ApplicationUser : IdentityUser
{
    [Required]
    [MaxLength(100)]
    public required string FirstName { get; set; }

    [Required]
    [MaxLength(100)]
    public required string LastName { get; set; }

    [Required]
    [MaxLength(100)]
    public required string Academy { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}