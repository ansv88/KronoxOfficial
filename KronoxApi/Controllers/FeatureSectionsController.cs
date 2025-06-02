using KronoxApi.Attributes;
using KronoxApi.Data;
using KronoxApi.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KronoxApi.Models;

namespace KronoxApi.Controllers;

// API-kontroller för att hämta feature-sektioner för en given sida.
[ApiController]
[Route("api/features/{pageKey}")]
[RequireApiKey]
public class FeatureSectionsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<FeatureSectionsController> _logger;

    public FeatureSectionsController(
        ApplicationDbContext db,
        ILogger<FeatureSectionsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // Hämtar alla feature-sektioner för en viss sida (pageKey).
    [HttpGet]
    public async Task<IActionResult> GetFeatures(string pageKey)
    {
        try
        {
            var list = await _db.FeatureSections
                .Where(fs => fs.PageKey == pageKey)
                .OrderBy(fs => fs.SortOrder)
                .ToListAsync();

            if (list.Count == 0)
            {
                _logger.LogWarning("Inga feature-sektioner hittades för pageKey: {PageKey}", pageKey);
                return NotFound($"Inga feature-sektioner hittades för sidan '{pageKey}'.");
            }

            var dtos = list.Select(fs => new FeatureSectionDto
            {
                Id = fs.Id,
                Title = fs.Title,
                Content = fs.Content,
                ImageUrl = fs.ImageUrl,
                ImageAltText = fs.ImageAltText,
                HasImage = fs.HasImage,
                SortOrder = fs.SortOrder
            }).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid hämtning av feature-sektioner för pageKey: {PageKey}", pageKey);
            return StatusCode(500, "Ett oväntat fel inträffade vid hämtning av feature-sektioner.");
        }
    }

    // Skapar eller uppdaterar feature-sektioner för en sida
    [HttpPut]
    [RequireRole("Admin")]
    public async Task<IActionResult> UpdateFeatures(string pageKey, [FromBody] List<FeatureSectionDto> sections)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var existingSections = await _db.FeatureSections
                .Where(f => f.PageKey == pageKey)
                .ToListAsync();

            _db.FeatureSections.RemoveRange(existingSections);

            for (int i = 0; i < sections.Count; i++)
            {
                var dto = sections[i];
                var section = new FeatureSection
                {
                    PageKey = pageKey,
                    Title = dto.Title,
                    Content = dto.Content,
                    ImageUrl = dto.ImageUrl,
                    ImageAltText = dto.ImageAltText,
                    HasImage = dto.HasImage,
                    SortOrder = i
                };

                _db.FeatureSections.Add(section);
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation("Uppdaterade {Count} feature-sektioner för sidan {PageKey}",
                sections.Count, pageKey);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid uppdatering av feature-sektioner för {PageKey}", pageKey);
            return StatusCode(500, "Ett fel inträffade vid uppdatering av feature-sektioner");
        }
    }
}