using Microsoft.AspNetCore.Identity;

namespace KronoxApi.Services;

// Översätter ASP.NET Identity-felmeddelanden till svenska.

public class SwedishIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError()
        => new() { Code = nameof(DefaultError), Description = "Ett okänt fel har inträffat." };

    public override IdentityError ConcurrencyFailure()
        => new() { Code = nameof(ConcurrencyFailure), Description = "Ett tillfälligt fel uppstod. Försök igen." };

    public override IdentityError PasswordMismatch()
        => new() { Code = nameof(PasswordMismatch), Description = "Felaktigt lösenord." };

    public override IdentityError InvalidToken()
        => new() { Code = nameof(InvalidToken), Description = "Ogiltig token." };

    public override IdentityError LoginAlreadyAssociated()
        => new() { Code = nameof(LoginAlreadyAssociated), Description = "En användare med denna inloggning finns redan." };

    public override IdentityError InvalidUserName(string? userName)
        => new() { Code = nameof(InvalidUserName), Description = $"Användarnamnet '{userName}' är ogiltigt. Det får bara innehålla bokstäver och siffror." };

    public override IdentityError InvalidEmail(string? email)
        => new() { Code = nameof(InvalidEmail), Description = $"E-postadressen '{email}' är ogiltig." };

    public override IdentityError DuplicateUserName(string userName)
        => new() { Code = nameof(DuplicateUserName), Description = $"Användarnamnet '{userName}' är redan upptaget." };

    public override IdentityError DuplicateEmail(string email)
        => new() { Code = nameof(DuplicateEmail), Description = $"E-postadressen '{email}' är redan registrerad." };

    public override IdentityError InvalidRoleName(string? role)
        => new() { Code = nameof(InvalidRoleName), Description = $"Rollnamnet '{role}' är ogiltigt." };

    public override IdentityError DuplicateRoleName(string role)
        => new() { Code = nameof(DuplicateRoleName), Description = $"Rollnamnet '{role}' är redan upptaget." };

    public override IdentityError UserAlreadyHasPassword()
        => new() { Code = nameof(UserAlreadyHasPassword), Description = "Användaren har redan ett lösenord." };

    public override IdentityError UserLockoutNotEnabled()
        => new() { Code = nameof(UserLockoutNotEnabled), Description = "Kontoutelåsning är inte aktiverat för denna användare." };

    public override IdentityError UserAlreadyInRole(string role)
        => new() { Code = nameof(UserAlreadyInRole), Description = $"Användaren har redan rollen '{role}'." };

    public override IdentityError UserNotInRole(string role)
        => new() { Code = nameof(UserNotInRole), Description = $"Användaren har inte rollen '{role}'." };

    public override IdentityError PasswordTooShort(int length)
        => new() { Code = nameof(PasswordTooShort), Description = $"Lösenordet måste vara minst {length} tecken långt." };

    public override IdentityError PasswordRequiresNonAlphanumeric()
        => new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "Lösenordet måste innehålla minst ett specialtecken (t.ex. !@#$%)." };

    public override IdentityError PasswordRequiresDigit()
        => new() { Code = nameof(PasswordRequiresDigit), Description = "Lösenordet måste innehålla minst en siffra (0–9)." };

    public override IdentityError PasswordRequiresLower()
        => new() { Code = nameof(PasswordRequiresLower), Description = "Lösenordet måste innehålla minst en liten bokstav (a–z)." };

    public override IdentityError PasswordRequiresUpper()
        => new() { Code = nameof(PasswordRequiresUpper), Description = "Lösenordet måste innehålla minst en stor bokstav (A–Z)." };

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars)
        => new() { Code = nameof(PasswordRequiresUniqueChars), Description = $"Lösenordet måste innehålla minst {uniqueChars} unika tecken." };
}