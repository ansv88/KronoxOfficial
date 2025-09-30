using KronoxApi.Attributes;
using KronoxApi.DTOs;
using KronoxApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace KronoxApi.Controllers;

/// <summary>
/// Hanterar autentisering och registrering av användare.
/// </summary>

[ApiController]
[Route("api/[controller]")]
[RequireApiKey]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly ILogger<AuthController> _logger;
    private const string NewUserRole = "Ny användare";

    public AuthController(
        UserManager<ApplicationUser> users,
        SignInManager<ApplicationUser> signIn,
        ILogger<AuthController> logger)
    {
        _users = users;
        _signIn = signIn;
        _logger = logger;
    }

    // Loggar in en användare med användarnamn och lösenord.
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var user = await _users.FindByNameAsync(dto.Username);
        if (user == null || !await _users.CheckPasswordAsync(user, dto.Password))
        {
            _logger.LogWarning("Inloggning misslyckades för {User}", dto.Username);
            return Unauthorized("Felaktigt användarnamn eller lösenord.");
        }

        var roles = await _users.GetRolesAsync(user);

        return Ok(new UserDto
        {
            UserId = user.Id,
            UserName = user.UserName!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email!,
            Roles = roles.ToList()
        });
    }

    // Registrerar en ny användare och lägger till rollen "Ny användare".
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var newUser = new ApplicationUser
        {
            UserName = dto.UserName,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Academy = dto.Academy,
            Email = dto.Email
        };
        var createResult = await _users.CreateAsync(newUser, dto.Password);
        if (!createResult.Succeeded)
        {
            _logger.LogWarning("Registrering misslyckades för {User}: {Errors}",
                dto.UserName,
                string.Join(", ", createResult.Errors.Select(e => e.Description)));
            return BadRequest(string.Join(", ", createResult.Errors.Select(e => e.Description)));
        }

        await _users.AddToRoleAsync(newUser, NewUserRole);
        return Ok("Användare skapad.");
    }

    // Returnerar lösenordskraven för registrering.
    [HttpGet("password-requirements")]
    public IActionResult GetPasswordRequirements()
    {
        var opts = _users.Options.Password;
        return Ok(RegisterDto.GetPasswordRequirements(opts));
    }
}