using KronoxApi.Attributes;
using KronoxApi.DTOs;
using KronoxApi.Models;
using KronoxApi.Requests;
using KronoxApi.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace KronoxApi.Controllers;

/// <summary>
/// Controller för administrativa åtgärder i systemet, kräver Admin-roll och en giltig API-nyckel
/// </summary>

[ApiController]
[Route("api/[controller]")]
[RequireRole("Admin")]
[RequireApiKey]
[EnableRateLimiting("Admin")]
public class AdminController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<AdminController> _logger;
    private readonly EmailTemplates _emailTemplates;
    private const string NewUserRole = "Ny användare";

    // Konstruktor för AdminControll
    public AdminController(
              UserManager<ApplicationUser> userManager,
              RoleManager<IdentityRole> roleManager,
              IEmailService emailService,
              ILogger<AdminController> logger,
              IOptions<EmailTemplates> emailTemplatesOptions)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _emailTemplates = emailTemplatesOptions?.Value ?? throw new ArgumentNullException(nameof(emailTemplatesOptions));
    }


    // Hämtar alla tillgängliga roller i systemet, returnerar en lista med rollnamn
    [HttpGet("roles")]
    public IActionResult GetAvailableRoles()
    {
        try
        {
            var roles = _roleManager.Roles.Select(r => r.Name).ToList();

            if (!roles.Any())
            {
                return NoContent();
            }

            return Ok(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ett fel inträffade vid hämtning av roller");
            return StatusCode(StatusCodes.Status500InternalServerError, "Ett oväntat fel inträffade vid hämtning av roller");
        }
    }


    // Hämtar alla användare med rollen "Ny användare", lista med nya användare som väntar på godkännande
    [HttpGet("registration-requests")]
    public async Task<IActionResult> GetRegistrationRequests()
    {
        try
        {
            var newUsers = await _userManager.GetUsersInRoleAsync(NewUserRole);
            var result = new List<AdminUserDto>(newUsers.Count);

            foreach (var user in newUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                result.Add(new AdminUserDto
                {
                    UserName = user.UserName,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Academy = user.Academy,
                    Roles = roles.ToList(),
                    CreatedDate = user.CreatedDate
                });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ett fel inträffade vid hämtning av registreringsförfrågningar");
            return StatusCode(StatusCodes.Status500InternalServerError, "Ett oväntat fel inträffade vid hämtning av registreringsförfrågningar");
        }
    }


    // Godkänner en ny användare genom att tilldela den en angiven roll och ta bort "Ny användare"-rollen
    [HttpPost("approve-user")]
    public async Task<IActionResult> ApproveUser([FromBody] UserRoleChangeRequest model)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Ogiltig modell vid godkännande av användare");
            return BadRequest(ModelState);
        }

        try
        {
            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user == null)
            {
                return NotFound($"Användare '{model.UserName}' hittades inte");
            }

            // Ta bort rollen "Ny användare" om användaren redan har den
            if (await _userManager.IsInRoleAsync(user, NewUserRole))
            {
                await _userManager.RemoveFromRoleAsync(user, NewUserRole);
            }

            // Kolla om den nya rollen finns
            var roleExists = await _roleManager.RoleExistsAsync(model.RoleName);
            if (!roleExists)
            {
                return BadRequest($"Rollen '{model.RoleName}' existerar inte");
            }

            // Lägg till den nya rollen
            var result = await _userManager.AddToRoleAsync(user, model.RoleName);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Kunde inte tilldela roll {RoleName} till användare {UserName}. Fel: {Errors}",
                    model.RoleName, model.UserName, errors);
                return StatusCode(StatusCodes.Status500InternalServerError, $"Kunde inte tilldela rollen. Fel: {errors}");
            }

            // Skicka bekräftelsemail till användaren
            try
            {
                var subject = _emailTemplates.AccountApprovedSubject;
                var body = _emailTemplates.AccountApprovedBody
                    .Replace("{FirstName}", user.FirstName)
                    .Replace("{RoleName}", model.RoleName);

                await _emailService.SendEmailAsync(user.Email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ett fel inträffade vid e-postutskick till {Email}", user.Email);
                // Fortsätt utan att returnera fel - e-post är inte kritiskt för operationen
            }

            return StatusCode(StatusCodes.Status201Created, $"Användare '{model.UserName}' har nu rollen '{model.RoleName}'");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ett fel inträffade vid godkännande av användare {UserName}", model.UserName);
            return StatusCode(StatusCodes.Status500InternalServerError, "Ett oväntat fel inträffade vid godkännande av användare");
        }
    }


    // Tilldelar en ny roll till en användare och tar bort alla befintliga roller
    [HttpPost("assignrole")]
    public async Task<IActionResult> AssignRoleToUser([FromBody] UserRoleChangeRequest model)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Ogiltig modell vid tilldelning av roll");
            return BadRequest(ModelState);
        }

        try
        {
            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user == null)
            {
                return NotFound($"Användare '{model.UserName}' hittades inte");
            }

            // Kolla om rollen existerar
            var roleExists = await _roleManager.RoleExistsAsync(model.RoleName);
            if (!roleExists)
            {
                return BadRequest($"Rollen '{model.RoleName}' existerar inte");
            }

            // Hämta befintlig roll för användaren och ta bort den
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
                    _logger.LogError("Kunde inte ta bort tidigare roller från användare {UserName}. Fel: {Errors}",
                        model.UserName, errors);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        $"Kunde inte ta bort tidigare roller från användaren. Fel: {errors}");
                }
            }

            // Lägg till rollen
            var result = await _userManager.AddToRoleAsync(user, model.RoleName);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Kunde inte tilldela roll {RoleName} till användare {UserName}. Fel: {Errors}",
                    model.RoleName, model.UserName, errors);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"Kunde inte tilldela rollen till användaren. Fel: {errors}");
            }

            return Ok($"Användare '{model.UserName}' har nu rollen '{model.RoleName}'");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ett fel inträffade vid tilldelning av roll till användare {UserName}", model.UserName);
            return StatusCode(StatusCodes.Status500InternalServerError, "Ett oväntat fel inträffade vid tilldelning av roll");
        }
    }


    // Uppdaterar en användares profilinformation
    [HttpPut("update-user/{userName}")]
    public async Task<IActionResult> UpdateUserProfile(string userName, [FromBody] UpdateUserRequest model)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Ogiltig modell vid uppdatering av användarprofil");
            return BadRequest(ModelState);
        }

        try
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                return NotFound($"Användaren '{userName}' hittades inte");
            }

            // Uppdatera användarens fält
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Email = model.Email;
            user.Academy = model.Academy;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Kunde inte uppdatera användare {UserName}. Fel: {Errors}", userName, errors);
                return StatusCode(StatusCodes.Status500InternalServerError, $"Kunde inte uppdatera användaren. Fel: {errors}");
            }

            return Ok($"Användare '{userName}' har uppdaterats");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ett fel inträffade vid uppdatering av användarprofil för {UserName}", userName);
            return StatusCode(StatusCodes.Status500InternalServerError, "Ett oväntat fel inträffade vid uppdatering av användarprofil");
        }
    }


    // Tar bort en användare från systemet
    [HttpDelete("delete-user/{username}")]
    public async Task<IActionResult> DeleteUser(string username)
    {
        try
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
            {
                return NotFound($"Användare med användarnamn '{username}' hittades inte");
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Kunde inte ta bort användare {UserName}. Fel: {Errors}", username, errors);
                return StatusCode(StatusCodes.Status500InternalServerError, $"Kunde inte ta bort användaren. Fel: {errors}");
            }

            return Ok($"Användaren '{username}' har tagits bort från systemet");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ett fel inträffade vid borttagning av användare {UserName}", username);
            return StatusCode(StatusCodes.Status500InternalServerError, "Ett oväntat fel inträffade vid borttagning av användare");
        }
    }


    // Tar bort en registreringsförfrågan (användare med rollen "Ny användare")
    [HttpDelete("registration-request/{userName}")]
    public async Task<IActionResult> DeleteRegistrationRequest(string userName)
    {
        try
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                return NotFound($"Användare '{userName}' hittades inte");
            }

            // Kontrollera att användaren har rollen "Ny användare"
            if (!await _userManager.IsInRoleAsync(user, NewUserRole))
            {
                _logger.LogWarning("Användare {UserName} är inte en registreringsförfrågan", userName);
                return BadRequest($"Användaren '{userName}' är inte en registreringsförfrågan");
            }

            // Spara e-postadress och namn innan vi tar bort användaren
            string userEmail = user.Email;
            string firstName = user.FirstName;

            // Radera användaren från systemet
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Kunde inte ta bort registreringsförfrågan för {UserName}. Fel: {Errors}", userName, errors);
                return StatusCode(StatusCodes.Status500InternalServerError, $"Kunde inte ta bort registreringsförfrågan. Fel: {errors}");
            }

            return Ok($"Registreringsförfrågan för '{userName}' borttagen");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ett fel inträffade vid borttagning av registreringsförfrågan för {UserName}", userName);
            return StatusCode(StatusCodes.Status500InternalServerError, "Ett oväntat fel inträffade vid borttagning av registreringsförfrågan");
        }
    }


    // Återställer lösenordet för en användare
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetUserPassword([FromBody] ResetPasswordRequest model)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Ogiltig modell vid återställning av lösenord");
            return BadRequest(ModelState);
        }

        try
        {
            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user == null)
            {
                return NotFound($"Användare '{model.UserName}' hittades inte");
            }

            // Ta bort befintligt lösenord och lägg till ett nytt
            await _userManager.RemovePasswordAsync(user);
            var result = await _userManager.AddPasswordAsync(user, model.NewPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Kunde inte ändra lösenord för användare {UserName}. Fel: {Errors}", model.UserName, errors);
                return StatusCode(StatusCodes.Status500InternalServerError, $"Kunde inte ändra lösenordet. Fel: {errors}");
            }

            return Ok($"Lösenordet har ändrats för användare '{model.UserName}'");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ett fel inträffade vid ändring av lösenord för {UserName}", model.UserName);
            return StatusCode(StatusCodes.Status500InternalServerError, "Ett oväntat fel inträffade vid ändring av lösenord");
        }
    }


    // Hämtar alla användare i systemet med deras roller
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        try
        {
            var users = _userManager.Users.ToList();
            var result = new List<AdminUserDto>(users.Count);

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                result.Add(new AdminUserDto
                {
                    UserName = user.UserName,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Academy = user.Academy,
                    Roles = roles.ToList(),
                    CreatedDate = user.CreatedDate
                });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ett fel inträffade vid hämtning av användare");
            return StatusCode(StatusCodes.Status500InternalServerError, "Ett oväntat fel inträffade vid hämtning av användare");
        }
    }
}