using KronoxApi.Attributes;
using KronoxApi.Data;
using KronoxApi.DTOs;
using KronoxApi.Models;
using KronoxApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KronoxApi.Controllers;

[ApiController]
[RequireApiKey]
[Route("api/cms/logos")]
public class MemberLogoController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IFileService _files;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<MemberLogoController> _logger;

    public MemberLogoController(
        ApplicationDbContext db,
        IFileService files,
        IWebHostEnvironment env,
        ILogger<MemberLogoController> logger)
    {
        _db = db;
        _files = files;
        _env = env;
        _logger = logger;
    }


    // Hämtar alla medlemslogotyper sorterade efter visningsordning (tillgänglig för alla användare utan autentisering)
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var logos = await _db.MemberLogos
                                .OrderBy(l => l.SortOrd)
                                .ToListAsync();
            return Ok(logos.Select(l => new MemberLogoDto(l)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid hämtning av logotyper");
            return StatusCode(500, "Ett fel uppstod vid hämtning av logotyper");
        }
    }


    // Registrerar en befintlig bild som medlemslogotyp. Används när bilden redan finns på servern under wwwroot/images/members.
    [HttpPost("register")]
    [RequireRole("Admin")]
    public async Task<IActionResult> Register([FromBody] RegisterMemberLogoDto dto)
    {
        if (dto == null)
        {
            return BadRequest("Ogiltig indata");
        }

        try
        {
            // Räkna fram en fysisk sökväg
            var phys = Path.Combine(_env.WebRootPath, dto.SourcePath.TrimStart('/'));
            if (!System.IO.File.Exists(phys))
            {
                _logger.LogWarning("Fil hittades inte: {FilePath}", phys);
                return NotFound("Filen finns inte på servern");
            }

            // Packa in den lokala filen i ett FormFile
            var stream = System.IO.File.OpenRead(phys);
            var formFile = new FormFile(stream, 0, stream.Length, "file", dto.OriginalFileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/octet-stream"
            };

            // Validera och spara
            var err = _files.ValidateImage(formFile);
            if (err != null)
            {
                _logger.LogWarning("Bildvalidering misslyckades: {Error}", err);
                return BadRequest(err);
            }

            var url = await _files.SaveMemberLogoAsync(formFile);

            // Skapa DB-post
            var nextSort = (_db.MemberLogos.Max(l => (int?)l.SortOrd) ?? 0) + 1;
            var memberLogo = new MemberLogo
            {
                Url = url,
                AltText = dto.AltText,
                SortOrd = dto.SortOrd > 0 ? dto.SortOrd : nextSort,
                LinkUrl = dto.LinkUrl
            };

            _db.MemberLogos.Add(memberLogo);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Logotyp registrerad: {Url}", url);
            return Ok(new MemberLogoDto(memberLogo));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid registrering av logotyp");
            return StatusCode(500, "Ett fel uppstod vid registrering av logotypen");
        }
    }


    // Laddar upp en ny bild som medlemslogotyp. Tar emot en fil via multipart/form-data tillsammans med metadata.
    [HttpPost("upload")]
    [RequireRole("Admin")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] MemberLogoUploadDto dto)
    {
        if (dto == null || dto.File == null)
        {
            return BadRequest("Fil krävs");
        }

        try
        {
            var err = _files.ValidateImage(dto.File);
            if (err != null)
            {
                _logger.LogWarning("Bildvalidering misslyckades: {Error}", err);
                return BadRequest(err);
            }

            var url = await _files.SaveMemberLogoAsync(dto.File);
            var nextSort = (_db.MemberLogos.Max(l => (int?)l.SortOrd) ?? 0) + 1;
            var memberLogo = new MemberLogo
            {
                Url = url,
                AltText = dto.AltText,
                SortOrd = dto.SortOrd > 0 ? dto.SortOrd : nextSort,
                LinkUrl = dto.LinkUrl
            };

            _db.MemberLogos.Add(memberLogo);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Ny logotyp uppladdad: {Url}", url);
            return Ok(new MemberLogoDto(memberLogo));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid uppladdning av logotyp");
            return StatusCode(500, "Ett fel uppstod vid uppladdning av logotypen");
        }
    }


    // Uppdaterar beskrivningen (alt-text) för en specifik medlemslogotyp.
    [HttpPut("{id}/description")]
    [RequireRole("Admin")]
    public async Task<IActionResult> UpdateDescription(int id, [FromBody] DescriptionUpdateDto dto)
    {
        if (dto == null)
        {
            return BadRequest("Beskrivningsdata krävs");
        }

        try
        {
            var memberLogo = await _db.MemberLogos.FindAsync(id);
            if (memberLogo == null)
            {
                _logger.LogWarning("Logotyp hittades inte: {Id}", id);
                return NotFound("Logotypen hittades inte");
            }

            memberLogo.AltText = dto.Description;
            await _db.SaveChangesAsync();

            _logger.LogInformation("Beskrivning uppdaterad för logotyp: {Id}", id);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid uppdatering av beskrivning för logotyp: {Id}", id);
            return StatusCode(500, "Ett fel uppstod vid uppdatering av beskrivningen");
        }
    }


    // Uppdaterar webbadressen (länken) för en specifik medlemslogotyp. Används för att styra vart användaren hamnar vid klick på logotypen.
    [HttpPut("{id}/link")]
    [RequireRole("Admin")]
    public async Task<IActionResult> UpdateLink(int id, [FromBody] LinkUrlUpdateDto dto)
    {
        if (dto == null)
        {
            return BadRequest("Länkdata krävs");
        }

        try
        {
            var memberLogo = await _db.MemberLogos.FindAsync(id);
            if (memberLogo == null)
            {
                _logger.LogWarning("Logotyp hittades inte: {Id}", id);
                return NotFound("Logotypen hittades inte");
            }

            memberLogo.LinkUrl = dto.LinkUrl;
            await _db.SaveChangesAsync();

            _logger.LogInformation("Länk uppdaterad för logotyp: {Id}", id);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid uppdatering av länk för logotyp: {Id}", id);
            return StatusCode(500, "Ett fel uppstod vid uppdatering av länken");
        }
    }


    // Flyttar en medlemslogotyp uppåt eller nedåt i visningsordningen.
    [HttpPost("move")]
    [RequireRole("Admin")]
    public async Task<IActionResult> Move([FromBody] LogoMoveDto dto)
    {
        if (dto == null)
        {
            return BadRequest("Flyttdata krävs");
        }

        try
        {
            if (dto.Direction != -1 && dto.Direction != 1)
            {
                _logger.LogWarning("Ogiltig flyttriktning: {Direction}", dto.Direction);
                return BadRequest("Riktningen måste vara -1 (upp) eller 1 (ner)");
            }

            var logoToMove = await _db.MemberLogos.FindAsync(dto.LogoId);
            if (logoToMove == null)
            {
                _logger.LogWarning("Logotyp att flytta hittades inte: {Id}", dto.LogoId);
                return NotFound("Logotypen hittades inte");
            }

            var allLogos = await _db.MemberLogos
                .OrderBy(l => l.SortOrd)
                .ToListAsync();

            int currentIndex = allLogos.FindIndex(l => l.Id == dto.LogoId);
            int targetIndex = currentIndex + dto.Direction;

            if (targetIndex < 0 || targetIndex >= allLogos.Count)
            {
                return BadRequest("Kan inte flytta logotypen längre i den riktningen");
            }

            var targetLogo = allLogos[targetIndex];

            int temp = logoToMove.SortOrd;
            logoToMove.SortOrd = targetLogo.SortOrd;
            targetLogo.SortOrd = temp;

            await _db.SaveChangesAsync();

            _logger.LogInformation("Logotyp {LogoId} flyttad {Direction}",
                dto.LogoId, dto.Direction == -1 ? "upp" : "ner");
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid flytt av logotyp: {LogoId}", dto.LogoId);
            return StatusCode(500, "Ett fel uppstod vid flytt av logotypen");
        }
    }


    // Tar bort en medlemslogotyp från systemet. Raderar både databasposten och bildfilen från servern.
    [HttpDelete("{id}")]
    [RequireRole("Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var memberLogo = await _db.MemberLogos.FindAsync(id);
            if (memberLogo == null)
            {
                _logger.LogWarning("Logotyp hittades inte för radering: {Id}", id);
                return NotFound("Logotypen hittades inte");
            }

            await _files.DeleteMemberLogoAsync(memberLogo.Url);
            _db.MemberLogos.Remove(memberLogo);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Logotyp raderad: {Id}", id);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid radering av logotyp: {Id}", id);
            return StatusCode(500, "Ett fel uppstod vid radering av logotypen");
        }
    }
}