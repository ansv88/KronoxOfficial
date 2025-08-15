using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using KronoxApi.Data;
using KronoxApi.Models;
using KronoxApi.DTOs;
using KronoxApi.Attributes;
using KronoxApi.Filters;

namespace KronoxApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[RequireApiKey]
public class FaqController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FaqController> _logger;

    public FaqController(ApplicationDbContext context, ILogger<FaqController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/faq/{pageKey}
    [HttpGet("{pageKey}")]
    public async Task<ActionResult<List<FaqSectionDto>>> GetFaqSections(string pageKey)
    {
        try
        {
            var sections = await _context.FaqSections
                .Where(fs => fs.PageKey == pageKey)
                .Include(fs => fs.FaqItems.OrderBy(fi => fi.SortOrder))
                .OrderBy(fs => fs.SortOrder)
                .ToListAsync();

            var sectionDtos = sections.Select(s => new FaqSectionDto
            {
                Id = s.Id,
                PageKey = s.PageKey,
                Title = s.Title,
                Description = s.Description,
                SortOrder = s.SortOrder,
                FaqItems = s.FaqItems.Select(fi => new FaqItemDto
                {
                    Id = fi.Id,
                    FaqSectionId = fi.FaqSectionId,
                    Question = fi.Question,
                    Answer = fi.Answer,
                    ImageUrl = fi.ImageUrl,
                    ImageAltText = fi.ImageAltText,
                    HasImage = fi.HasImage,
                    SortOrder = fi.SortOrder
                }).ToList()
            }).ToList();

            return Ok(sectionDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid hämtning av FAQ-sektioner för {PageKey}", pageKey);
            return StatusCode(500, "Ett fel uppstod vid hämtning av FAQ-innehåll");
        }
    }

    // POST: api/faq/section
    [HttpPost("section")]
    [RequireRole("Admin")]
    public async Task<ActionResult<FaqSectionDto>> CreateFaqSection(CreateFaqSectionDto dto)
    {
        try
        {
            var section = new FaqSection
            {
                PageKey = dto.PageKey,
                Title = dto.Title,
                Description = dto.Description,
                SortOrder = dto.SortOrder
            };

            _context.FaqSections.Add(section);
            await _context.SaveChangesAsync();

            var sectionDto = new FaqSectionDto
            {
                Id = section.Id,
                PageKey = section.PageKey,
                Title = section.Title,
                Description = section.Description,
                SortOrder = section.SortOrder,
                FaqItems = new List<FaqItemDto>()
            };

            return CreatedAtAction(nameof(GetFaqSections), new { pageKey = section.PageKey }, sectionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid skapande av FAQ-sektion");
            return StatusCode(500, "Ett fel uppstod vid skapande av FAQ-sektion");
        }
    }

    // POST: api/faq/item
    [HttpPost("item")]
    [RequireRole("Admin")]
    public async Task<ActionResult<FaqItemDto>> CreateFaqItem(CreateFaqItemDto dto)
    {
        try
        {
            var item = new FaqItem
            {
                FaqSectionId = dto.FaqSectionId,
                Question = dto.Question,
                Answer = dto.Answer,
                ImageUrl = dto.ImageUrl,
                ImageAltText = dto.ImageAltText,
                HasImage = dto.HasImage,
                SortOrder = dto.SortOrder
            };

            _context.FaqItems.Add(item);
            await _context.SaveChangesAsync();

            var itemDto = new FaqItemDto
            {
                Id = item.Id,
                FaqSectionId = item.FaqSectionId,
                Question = item.Question,
                Answer = item.Answer,
                ImageUrl = item.ImageUrl,
                ImageAltText = item.ImageAltText,
                HasImage = item.HasImage,
                SortOrder = item.SortOrder
            };

            return CreatedAtAction(nameof(GetFaqSections), new { pageKey = "dummy" }, itemDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid skapande av FAQ-item");
            return StatusCode(500, "Ett fel uppstod vid skapande av FAQ-item");
        }
    }

    // PUT: api/faq/section/{id}
    [HttpPut("section/{id}")]
    [RequireRole("Admin")]
    public async Task<ActionResult<FaqSectionDto>> UpdateFaqSection(int id, FaqSectionDto dto)
    {
        try
        {
            var section = await _context.FaqSections.FindAsync(id);
            if (section == null)
                return NotFound($"FAQ-sektion med ID {id} hittades inte");

            section.Title = dto.Title;
            section.Description = dto.Description;
            section.SortOrder = dto.SortOrder;

            await _context.SaveChangesAsync();

            var updatedDto = new FaqSectionDto
            {
                Id = section.Id,
                PageKey = section.PageKey,
                Title = section.Title,
                Description = section.Description,
                SortOrder = section.SortOrder,
                FaqItems = new List<FaqItemDto>()
            };

            return Ok(updatedDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid uppdatering av FAQ-sektion {Id}", id);
            return StatusCode(500, "Ett fel uppstod vid uppdatering av FAQ-sektion");
        }
    }

    // PUT: api/faq/item/{id}
    [HttpPut("item/{id}")]
    [RequireRole("Admin")]
    public async Task<ActionResult<FaqItemDto>> UpdateFaqItem(int id, FaqItemDto dto)
    {
        try
        {
            var item = await _context.FaqItems.FindAsync(id);
            if (item == null)
                return NotFound($"FAQ-item med ID {id} hittades inte");

            item.Question = dto.Question;
            item.Answer = dto.Answer;
            item.ImageUrl = dto.ImageUrl;
            item.ImageAltText = dto.ImageAltText;
            item.HasImage = dto.HasImage;
            item.SortOrder = dto.SortOrder;

            await _context.SaveChangesAsync();

            var updatedDto = new FaqItemDto
            {
                Id = item.Id,
                FaqSectionId = item.FaqSectionId,
                Question = item.Question,
                Answer = item.Answer,
                ImageUrl = item.ImageUrl,
                ImageAltText = item.ImageAltText,
                HasImage = item.HasImage,
                SortOrder = item.SortOrder
            };

            return Ok(updatedDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid uppdatering av FAQ-item {Id}", id);
            return StatusCode(500, "Ett fel uppstod vid uppdatering av FAQ-item");
        }
    }

    // DELETE: api/faq/section/{id}
    [HttpDelete("section/{id}")]
    [RequireRole("Admin")]
    public async Task<ActionResult> DeleteFaqSection(int id)
    {
        try
        {
            var section = await _context.FaqSections
                .Include(fs => fs.FaqItems)
                .FirstOrDefaultAsync(fs => fs.Id == id);

            if (section == null)
                return NotFound($"FAQ-sektion med ID {id} hittades inte");

            _context.FaqItems.RemoveRange(section.FaqItems);
            _context.FaqSections.Remove(section);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid borttagning av FAQ-sektion {Id}", id);
            return StatusCode(500, "Ett fel uppstod vid borttagning av FAQ-sektion");
        }
    }

    // DELETE: api/faq/item/{id}
    [HttpDelete("item/{id}")]
    [RequireRole("Admin")]
    public async Task<ActionResult> DeleteFaqItem(int id)
    {
        try
        {
            var item = await _context.FaqItems.FindAsync(id);
            if (item == null)
                return NotFound($"FAQ-item med ID {id} hittades inte");

            _context.FaqItems.Remove(item);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid borttagning av FAQ-item {Id}", id);
            return StatusCode(500, "Ett fel uppstod vid borttagning av FAQ-item");
        }
    }

    // DELETE: api/faq/page/{pageKey}
    [HttpDelete("page/{pageKey}")]
    [RequireRole("Admin")]
    public async Task<ActionResult> DeleteFaqSectionsByPage(string pageKey)
    {
        try
        {
            var sections = await _context.FaqSections
                .Where(fs => fs.PageKey == pageKey)
                .Include(fs => fs.FaqItems)
                .ToListAsync();

            foreach (var section in sections)
            {
                _context.FaqItems.RemoveRange(section.FaqItems);
            }
            
            _context.FaqSections.RemoveRange(sections);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid borttagning av FAQ-sektioner för {PageKey}", pageKey);
            return StatusCode(500, "Ett fel uppstod vid borttagning av FAQ-sektioner");
        }
    }

    // PUT: api/faq/page/{pageKey}
    [HttpPut("page/{pageKey}")]
    [RequireRole("Admin")]
    public async Task<ActionResult> UpdatePageFaqSections(string pageKey, List<FaqSectionDto> sectionsDto)
    {
        try
        {
            // Ta bort alla befintliga FAQ-sektioner för sidan
            var existingSections = await _context.FaqSections
                .Where(fs => fs.PageKey == pageKey)
                .Include(fs => fs.FaqItems)
                .ToListAsync();

            foreach (var section in existingSections)
            {
                _context.FaqItems.RemoveRange(section.FaqItems);
            }
            _context.FaqSections.RemoveRange(existingSections);

            // Lägg till nya sektioner
            foreach (var sectionDto in sectionsDto)
            {
                var section = new FaqSection
                {
                    PageKey = pageKey,
                    Title = sectionDto.Title,
                    Description = sectionDto.Description,
                    SortOrder = sectionDto.SortOrder,
                    FaqItems = sectionDto.FaqItems.Select(itemDto => new FaqItem
                    {
                        Question = itemDto.Question,
                        Answer = itemDto.Answer,
                        ImageUrl = itemDto.ImageUrl,
                        ImageAltText = itemDto.ImageAltText,
                        HasImage = itemDto.HasImage,
                        SortOrder = itemDto.SortOrder
                    }).ToList()
                };

                _context.FaqSections.Add(section);
            }

            await _context.SaveChangesAsync();
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid uppdatering av FAQ-sektioner för {PageKey}", pageKey);
            return StatusCode(500, "Ett fel uppstod vid uppdatering av FAQ-sektioner");
        }
    }
}