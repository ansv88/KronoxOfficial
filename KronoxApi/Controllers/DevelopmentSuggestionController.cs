using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KronoxApi.Data;
using KronoxApi.DTOs;
using KronoxApi.Models;
using KronoxApi.Services;
using KronoxApi.Attributes;

namespace KronoxApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[RequireApiKey]
public class DevelopmentSuggestionController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<DevelopmentSuggestionController> _logger;

    public DevelopmentSuggestionController(
        ApplicationDbContext context, 
        IEmailService emailService,
        ILogger<DevelopmentSuggestionController> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> SubmitSuggestion(DevelopmentSuggestionDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var suggestion = new DevelopmentSuggestion
            {
                Organization = dto.Organization,
                Name = dto.Name,
                Email = dto.Email,
                Requirement = dto.Requirement,
                ExpectedBenefit = dto.ExpectedBenefit,
                AdditionalInfo = dto.AdditionalInfo,
                SubmittedAt = DateTime.UtcNow
            };

            _context.DevelopmentSuggestions.Add(suggestion);
            await _context.SaveChangesAsync();

            // Skicka e-post
            await SendDevelopmentSuggestionEmail(suggestion);

            return Ok(new { message = "Utvecklingsf�rslaget har skickats." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid skickande av utvecklingsf�rslag");
            return StatusCode(500, "Ett fel uppstod vid skickande av f�rslaget");
        }
    }

    [HttpGet]
    [RequireRole("Admin")]
    public async Task<ActionResult<List<DevelopmentSuggestion>>> GetSuggestions(
        [FromQuery] bool includeProcessed = false)
    {
        var suggestions = await _context.DevelopmentSuggestions
            .Where(s => includeProcessed || !s.IsProcessed)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync();

        return suggestions;
    }

    [HttpPut("{id}/process")]
    public async Task<IActionResult> MarkAsProcessed(int id, [FromBody] ProcessSuggestionRequest request)
    {
        var suggestion = await _context.DevelopmentSuggestions.FindAsync(id);
        if (suggestion == null)
        {
            return NotFound();
        }

        suggestion.IsProcessed = true;
        suggestion.ProcessedBy = request.ProcessedBy;
        suggestion.ProcessedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Utvecklingsf�rslag {Id} markerat som behandlat av {ProcessedBy}", id, request.ProcessedBy);

        return Ok();
    }

    private async Task SendDevelopmentSuggestionEmail(DevelopmentSuggestion suggestion)
    {
        try
        {
            var subject = "Nytt utvecklingsf�rslag fr�n f�rvaltningssidan";
            var body = $@"
Ett nytt utvecklingsf�rslag har skickats via f�rvaltningssidan:

L�ros�te/Organisation: {suggestion.Organization}
Namn: {suggestion.Name}
E-post: {suggestion.Email}

Vad �r behovet?
{suggestion.Requirement}

Vilken effekt/nytta f�rv�ntas?
{suggestion.ExpectedBenefit}

Ytterligare information:
{suggestion.AdditionalInfo}

Skickat: {suggestion.SubmittedAt:yyyy-MM-dd HH:mm}
";

            await _emailService.SendEmailAsync("support@kronox.se", subject, body);
            _logger.LogInformation("Utvecklingsf�rslag-e-post skickad f�r f�rslag fr�n {Organization}", suggestion.Organization);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kunde inte skicka e-post f�r utvecklingsf�rslag fr�n {Organization}", suggestion.Organization);
        }
    }

    public class ProcessSuggestionRequest
    {
        public string ProcessedBy { get; set; } = "";
    }
}