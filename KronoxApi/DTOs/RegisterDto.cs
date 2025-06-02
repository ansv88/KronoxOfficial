using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace KronoxApi.DTOs;

// DTO för användarregistrering som fångar all information som behövs för att skapa ett nytt användarkonto
public class RegisterDto
{
    [Required(ErrorMessage = "Användarnamn krävs")]
    public required string UserName { get; set; }

    [Required(ErrorMessage = "Vänligen ange en e-postadress.")]
    [EmailAddress(ErrorMessage = "Ange en giltig e-postadress.")]
    public required string Email { get; set; }

    [Required(ErrorMessage = "Vänligen ange ett lösenord")]
    public required string Password { get; set; }

    [Required(ErrorMessage = "Vänligen bekräfta ditt lösenord.")]
    [Compare("Password", ErrorMessage = "Lösenorden matchar inte.")]
    public required string ConfirmPassword { get; set; }

    [Required(ErrorMessage = "Vänligen ange ditt förnamn.")]
    public required string FirstName { get; set; }

    [Required(ErrorMessage = "Vänligen ange ditt efternamn.")]
    public required string LastName { get; set; }

    [Required(ErrorMessage = "Vänligen ange ditt lärosäte.")]
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