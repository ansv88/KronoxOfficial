using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace KronoxApi.DTOs;

// DTO för användarregistrering som fångar all information som behövs för att skapa ett nytt användarkonto
public class RegisterDto
{
    [Required(ErrorMessage = "Användarnamn krävs")]
    [StringLength(100, ErrorMessage = "Användarnamnet är för långt.")]
    public required string UserName { get; set; }

    [Required(ErrorMessage = "Vänligen ange en e-postadress.")]
    [EmailAddress(ErrorMessage = "Ange en giltig e-postadress.")]
    [StringLength(100, ErrorMessage = "E-post får vara max 100 tecken.")]
    public required string Email { get; set; }

    [Required(ErrorMessage = "Vänligen ange ett lösenord")]
    [StringLength(256, ErrorMessage = "Lösenordet är för långt.")]
    public required string Password { get; set; }

    [Required(ErrorMessage = "Vänligen bekräfta ditt lösenord.")]
    [Compare("Password", ErrorMessage = "Lösenorden matchar inte.")]
    [StringLength(256, ErrorMessage = "Lösenordet är för långt.")]
    public required string ConfirmPassword { get; set; }

    [Required(ErrorMessage = "Vänligen ange ditt förnamn.")]
    [StringLength(100, ErrorMessage = "Förnamnet är för långt.")]
    public required string FirstName { get; set; }

    [Required(ErrorMessage = "Vänligen ange ditt efternamn.")]
    [StringLength(100, ErrorMessage = "Efternamnet är för långt.")]
    public required string LastName { get; set; }

    [Required(ErrorMessage = "Vänligen ange ditt lärosäte.")]
    [StringLength(200, ErrorMessage = "Lärosäte är för långt.")]
    public required string Academy { get; set; }

    // Genererar en användarvänlig beskrivning av lösenordskraven baserat på Identity-inställningarna
    public static string GetPasswordRequirements(PasswordOptions options)
    {
        var requirements = new List<string>
        {
            $"Minst {options.RequiredLength} tecken"
        };

        if (options.RequireUppercase)
            requirements.Add("Minst en stor bokstav");
        if (options.RequireLowercase)
            requirements.Add("Minst en liten bokstav");
        if (options.RequireDigit)
            requirements.Add("Minst en siffra");
        if (options.RequireNonAlphanumeric)
            requirements.Add("Minst ett specialtecken");

        return string.Join(", ", requirements);
    }
}