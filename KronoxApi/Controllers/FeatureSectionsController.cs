using KronoxApi.Data;
using KronoxApi.DTOs;
using KronoxApi.Models;
using KronoxApi.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace KronoxApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[RequireApiKey]
public class FeatureSectionsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<FeatureSectionsController> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public FeatureSectionsController(ApplicationDbContext db, ILogger<FeatureSectionsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // GET: api/featuresections/{pageKey} - Publikt tillgängligt (utan privat innehåll)
    [HttpGet("{pageKey}")]
    public async Task<ActionResult<List<FeatureSectionWithPrivateDto>>> GetFeatureSections(string pageKey)
    {
        try
        {
            var sections = await _db.FeatureSections
                .Where(fs => fs.PageKey == pageKey)
                .OrderBy(fs => fs.SortOrder)
                .ToListAsync();

            var dtos = sections.Select(s => new FeatureSectionWithPrivateDto
            {
                Id = s.Id,
                PageKey = s.PageKey,
                Title = s.Title,
                Content = s.Content,
                ImageUrl = s.ImageUrl,
                ImageAltText = s.ImageAltText,
                HasImage = s.HasImage,
                SortOrder = s.SortOrder,
                HasPrivateContent = s.HasPrivateContent,
                PrivateContent = "", // Skicka inte privat innehåll för publika anrop
                ContactPersonsJson = s.ContactPersonsJson, // Men skicka kontaktpersoner
                ContactHeading = s.ContactHeading,
                ContactPersons = !string.IsNullOrEmpty(s.ContactPersonsJson)
                    ? JsonSerializer.Deserialize<List<ContactPersonDto>>(s.ContactPersonsJson, _jsonOptions) ?? new()
                    : new()
            }).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid hämtning av feature-sektioner för {PageKey}", pageKey);
            return StatusCode(500, "Ett fel inträffade vid hämtning av feature-sektioner");
        }
    }

    // GET: api/featuresections/{pageKey}/authenticated - Kräver inloggning (medlem, styrelse eller admin)
    [HttpGet("{pageKey}/authenticated")]
    [RequireRole("Admin", "Medlem", "Styrelse")]
    public async Task<ActionResult<List<FeatureSectionWithPrivateDto>>> GetFeatureSectionsWithPrivate(string pageKey)
    {
        try
        {
            var sections = await _db.FeatureSections
                .Where(fs => fs.PageKey == pageKey)
                .OrderBy(fs => fs.SortOrder)
                .ToListAsync();

            var dtos = sections.Select(s => new FeatureSectionWithPrivateDto
            {
                Id = s.Id,
                PageKey = s.PageKey,
                Title = s.Title,
                Content = s.Content,
                ImageUrl = s.ImageUrl,
                ImageAltText = s.ImageAltText,
                HasImage = s.HasImage,
                SortOrder = s.SortOrder,
                HasPrivateContent = s.HasPrivateContent,
                PrivateContent = s.PrivateContent,
                ContactPersonsJson = s.ContactPersonsJson,
                ContactHeading = s.ContactHeading,
                ContactPersons = !string.IsNullOrEmpty(s.ContactPersonsJson)
                    ? JsonSerializer.Deserialize<List<ContactPersonDto>>(s.ContactPersonsJson, _jsonOptions) ?? new()
                    : new()
            }).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid hämtning av autentiserade feature-sektioner för {PageKey}", pageKey);
            return StatusCode(500, "Ett fel inträffade vid hämtning av feature-sektioner");
        }
    }

    // PUT: api/featuresections/{pageKey} - Endast Admin kan redigera
    [HttpPut("{pageKey}")]
    [RequireRole("Admin")]
    public async Task<IActionResult> UpdateFeatureSections(string pageKey, [FromBody] List<FeatureSectionWithPrivateDto> dtos)
    {
        try
        {
            _logger.LogInformation("Uppdaterar feature-sektioner för {PageKey} med {Count} sektioner", pageKey, dtos.Count);

            // Ta bort befintliga sektioner för denna sida
            var existingSections = await _db.FeatureSections
                .Where(fs => fs.PageKey == pageKey)
                .ToListAsync();

            _db.FeatureSections.RemoveRange(existingSections);

            // Lägg till nya sektioner
            for (int i = 0; i < dtos.Count; i++)
            {
                var dto = dtos[i];
                
                // Konvertera ContactPersons till JSON om det finns data där
                string contactPersonsJson = dto.ContactPersonsJson;
                if (dto.ContactPersons.Any() && string.IsNullOrEmpty(contactPersonsJson))
                {
                    contactPersonsJson = JsonSerializer.Serialize(dto.ContactPersons, _jsonOptions);
                }

                var section = new FeatureSection
                {
                    PageKey = pageKey,
                    Title = dto.Title,
                    Content = dto.Content,
                    ImageUrl = dto.ImageUrl,
                    ImageAltText = dto.ImageAltText,
                    HasImage = dto.HasImage,
                    SortOrder = i,
                    HasPrivateContent = dto.HasPrivateContent,
                    PrivateContent = dto.PrivateContent ?? "",
                    ContactPersonsJson = contactPersonsJson ?? "",
                    ContactHeading = dto.ContactHeading ?? ""
                };

                _db.FeatureSections.Add(section);
            }

            await _db.SaveChangesAsync();
            return Ok(new { message = "Feature-sektioner uppdaterade säkert" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid uppdatering av feature-sektioner för {PageKey}", pageKey);
            return StatusCode(500, "Ett fel inträffade vid uppdatering");
        }
    }
}