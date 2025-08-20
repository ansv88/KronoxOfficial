using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KronoxApi.DTOs;
using KronoxApi.Services;
using KronoxApi.Data;
using KronoxApi.Models;
using KronoxApi.Attributes;

namespace KronoxApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ContactController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ContactController> _logger;
    private readonly ApplicationDbContext _context;

    public ContactController(
        IEmailService emailService, 
        IConfiguration configuration, 
        ILogger<ContactController> logger,
        ApplicationDbContext context)
    {
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
        _context = context;
    }

    // ================== KONTAKTFORMULÄR ==================
    
    [HttpPost("send")]
    public async Task<IActionResult> SendContactMessage([FromBody] ContactFormDto contactForm)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // Hämta support-mailadress från konfiguration
            var supportEmail = _configuration["ContactSettings:SupportEmail"] ?? "support@kronox.se";
            
            // Skapa e-postinnehåll
            var emailSubject = $"Kontaktformulär: {contactForm.Subject}";
            var emailBody = $@"
Nytt meddelande från kontaktformuläret på KronoX-webbplatsen:

Från: {contactForm.Name}
E-post: {contactForm.Email}
Ämne: {contactForm.Subject}

Meddelande:
{contactForm.Message}

---
Detta meddelande skickades från kontaktformuläret på webbplatsen.
Svara direkt till {contactForm.Email} för att kontakta avsändaren.
";

            // Skicka e-post till support
            await _emailService.SendEmailAsync(supportEmail, emailSubject, emailBody);

            _logger.LogInformation("Kontaktmeddelande skickat från {Email} med ämne '{Subject}'", 
                contactForm.Email, contactForm.Subject);

            return Ok(new { message = "Ditt meddelande har skickats. Vi återkommer så snart som möjligt!" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid skickning av kontaktmeddelande från {Email}", contactForm.Email);
            return StatusCode(500, new { message = "Ett fel uppstod vid skickning av meddelandet. Försök igen senare." });
        }
    }

    // ================== KONTAKTINFORMATION ==================

    // GET: api/contact/info
    [HttpGet("info")]
    public async Task<ActionResult<ContactPageInfoDto>> GetContactInfo()
    {
        try
        {
            var postalAddress = await _context.PostalAddresses.FirstOrDefaultAsync() ?? new PostalAddress
            {
                OrganizationName = "KronoX",
                AddressLine1 = "Högskolan i Borås",
                PostalCode = "501 90",
                City = "Borås",
                Country = "Sverige"
            };

            var contactPersons = await _context.ContactPersons
                .Where(cp => cp.IsActive)
                .OrderBy(cp => cp.SortOrder)
                .ToListAsync();

            // Hämta e-postlistor
            var emailLists = await _context.EmailLists
                .Where(el => el.IsActive)
                .OrderBy(el => el.SortOrder)
                .ToListAsync();

            var result = new ContactPageInfoDto
            {
                PostalAddress = new ContactPostalAddressDto
                {
                    OrganizationName = postalAddress.OrganizationName,
                    AddressLine1 = postalAddress.AddressLine1,
                    AddressLine2 = postalAddress.AddressLine2,
                    PostalCode = postalAddress.PostalCode,
                    City = postalAddress.City,
                    Country = postalAddress.Country
                },
                ContactPersons = contactPersons.Select(cp => new ContactPagePersonDto
                {
                    Id = cp.Id,
                    Name = cp.Name,
                    Title = cp.Title,
                    Email = cp.Email,
                    Phone = cp.Phone,
                    SortOrder = cp.SortOrder,
                    IsActive = cp.IsActive
                }).ToList(),
                // Lägg till e-postlistor
                EmailLists = emailLists.Select(el => new EmailListDto
                {
                    Id = el.Id,
                    Name = el.Name,
                    Description = el.Description,
                    EmailAddress = el.EmailAddress,
                    SortOrder = el.SortOrder,
                    IsActive = el.IsActive
                }).ToList()
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid hämtning av kontaktinformation");
            return StatusCode(500, "Ett fel uppstod vid hämtning av kontaktinformation");
        }
    }

    // PUT: api/contact/postal-address
    [HttpPut("postal-address")]
    [RequireRole("Admin")]
    public async Task<ActionResult> UpdatePostalAddress(ContactPostalAddressDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var existingAddress = await _context.PostalAddresses.FirstOrDefaultAsync();
            
            if (existingAddress == null)
            {
                existingAddress = new PostalAddress();
                _context.PostalAddresses.Add(existingAddress);
            }

            existingAddress.OrganizationName = dto.OrganizationName;
            existingAddress.AddressLine1 = dto.AddressLine1;
            existingAddress.AddressLine2 = dto.AddressLine2;
            existingAddress.PostalCode = dto.PostalCode;
            existingAddress.City = dto.City;
            existingAddress.Country = dto.Country;
            existingAddress.LastModified = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Postadress uppdaterad av admin");
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid uppdatering av postadress");
            return StatusCode(500, "Ett fel uppstod vid uppdatering av postadress");
        }
    }

    // GET: api/contact/persons (för admin)
    [HttpGet("persons")]
    [RequireRole("Admin")]
    public async Task<ActionResult<List<ContactPagePersonDto>>> GetContactPersons()
    {
        try
        {
            var contactPersons = await _context.ContactPersons
                .OrderBy(cp => cp.SortOrder)
                .ThenBy(cp => cp.Name)
                .ToListAsync();

            var result = contactPersons.Select(cp => new ContactPagePersonDto
            {
                Id = cp.Id,
                Name = cp.Name,
                Title = cp.Title,
                Email = cp.Email,
                Phone = cp.Phone,
                SortOrder = cp.SortOrder,
                IsActive = cp.IsActive
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid hämtning av kontaktpersoner för admin");
            return StatusCode(500, "Ett fel uppstod vid hämtning av kontaktpersoner");
        }
    }

    // POST: api/contact/persons
    [HttpPost("persons")]
    [RequireRole("Admin")]
    public async Task<ActionResult<ContactPagePersonDto>> CreateContactPerson(UpsertContactPagePersonDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // Kontrollera om e-postadressen redan finns
            var existingPerson = await _context.ContactPersons
                .FirstOrDefaultAsync(cp => cp.Email.ToLower() == dto.Email.ToLower());
            
            if (existingPerson != null)
            {
                return BadRequest(new { message = "En kontaktperson med denna e-postadress finns redan." });
            }

            var contactPerson = new ContactPerson
            {
                Name = dto.Name,
                Title = dto.Title,
                Email = dto.Email,
                Phone = dto.Phone,
                SortOrder = dto.SortOrder,
                IsActive = dto.IsActive
            };

            _context.ContactPersons.Add(contactPerson);
            await _context.SaveChangesAsync();

            var result = new ContactPagePersonDto
            {
                Id = contactPerson.Id,
                Name = contactPerson.Name,
                Title = contactPerson.Title,
                Email = contactPerson.Email,
                Phone = contactPerson.Phone,
                SortOrder = contactPerson.SortOrder,
                IsActive = contactPerson.IsActive
            };

            _logger.LogInformation("Ny kontaktperson skapad: {Name} ({Email})", contactPerson.Name, contactPerson.Email);
            return CreatedAtAction(nameof(GetContactInfo), new { id = contactPerson.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid skapande av kontaktperson");
            return StatusCode(500, "Ett fel uppstod vid skapande av kontaktperson");
        }
    }

    // PUT: api/contact/persons/{id}
    [HttpPut("persons/{id}")]
    [RequireRole("Admin")]
    public async Task<ActionResult> UpdateContactPerson(int id, UpsertContactPagePersonDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var contactPerson = await _context.ContactPersons.FindAsync(id);
            if (contactPerson == null)
            {
                return NotFound(new { message = "Kontaktpersonen hittades inte." });
            }

            // Kontrollera om e-postadressen redan finns (utom för denna person)
            var existingPerson = await _context.ContactPersons
                .FirstOrDefaultAsync(cp => cp.Email.ToLower() == dto.Email.ToLower() && cp.Id != id);
            
            if (existingPerson != null)
            {
                return BadRequest(new { message = "En annan kontaktperson med denna e-postadress finns redan." });
            }

            contactPerson.Name = dto.Name;
            contactPerson.Title = dto.Title;
            contactPerson.Email = dto.Email;
            contactPerson.Phone = dto.Phone;
            contactPerson.SortOrder = dto.SortOrder;
            contactPerson.IsActive = dto.IsActive;
            contactPerson.LastModified = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Kontaktperson uppdaterad: {Name} (ID: {Id})", contactPerson.Name, id);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid uppdatering av kontaktperson med ID {Id}", id);
            return StatusCode(500, "Ett fel uppstod vid uppdatering av kontaktperson");
        }
    }

    // DELETE: api/contact/persons/{id}
    [HttpDelete("persons/{id}")]
    [RequireRole("Admin")]
    public async Task<ActionResult> DeleteContactPerson(int id)
    {
        try
        {
            var contactPerson = await _context.ContactPersons.FindAsync(id);
            if (contactPerson == null)
            {
                return NotFound(new { message = "Kontaktpersonen hittades inte." });
            }

            _context.ContactPersons.Remove(contactPerson);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Kontaktperson borttagen: {Name} (ID: {Id})", contactPerson.Name, id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid borttagning av kontaktperson med ID {Id}", id);
            return StatusCode(500, "Ett fel uppstod vid borttagning av kontaktperson");
        }
    }

    // PUT: api/contact/persons/{id}/toggle-active
    [HttpPut("persons/{id}/toggle-active")]
    [RequireRole("Admin")]
    public async Task<ActionResult> ToggleContactPersonActive(int id)
    {
        try
        {
            var contactPerson = await _context.ContactPersons.FindAsync(id);
            if (contactPerson == null)
            {
                return NotFound(new { message = "Kontaktpersonen hittades inte." });
            }

            contactPerson.IsActive = !contactPerson.IsActive;
            contactPerson.LastModified = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Kontaktperson {Name} (ID: {Id}) {Status}", 
                contactPerson.Name, id, contactPerson.IsActive ? "aktiverad" : "inaktiverad");
            
            return Ok(new { isActive = contactPerson.IsActive });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid ändring av aktivstatus för kontaktperson med ID {Id}", id);
            return StatusCode(500, "Ett fel uppstod vid ändring av aktivstatus");
        }
    }

    // ================== E-POSTLISTOR ==================

    // GET: api/contact/emaillists
    [HttpGet("emaillists")]
    public async Task<ActionResult<List<EmailListDto>>> GetEmailLists()
    {
        try
        {
            var emailLists = await _context.EmailLists
                .Where(el => el.IsActive)
                .OrderBy(el => el.SortOrder)
                .ToListAsync();

            var result = emailLists.Select(el => new EmailListDto
            {
                Id = el.Id,
                Name = el.Name,
                Description = el.Description,
                EmailAddress = el.EmailAddress,
                SortOrder = el.SortOrder,
                IsActive = el.IsActive
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid hämtning av e-postlistor");
            return StatusCode(500, "Ett fel uppstod vid hämtning av e-postlistor");
        }
    }

    // POST: api/contact/emaillists
    [HttpPost("emaillists")]
    [RequireRole("Admin")]
    public async Task<ActionResult<EmailListDto>> CreateEmailList(UpsertEmailListDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var emailList = new EmailList
            {
                Name = dto.Name,
                Description = dto.Description,
                EmailAddress = dto.EmailAddress,
                SortOrder = dto.SortOrder,
                IsActive = dto.IsActive
            };

            _context.EmailLists.Add(emailList);
            await _context.SaveChangesAsync();

            var result = new EmailListDto
            {
                Id = emailList.Id,
                Name = emailList.Name,
                Description = emailList.Description,
                EmailAddress = emailList.EmailAddress,
                SortOrder = emailList.SortOrder,
                IsActive = emailList.IsActive
            };

            _logger.LogInformation("Ny e-postlista skapad: {Name} ({Email})", emailList.Name, emailList.EmailAddress);
            return CreatedAtAction(nameof(GetEmailLists), new { id = emailList.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid skapande av e-postlista");
            return StatusCode(500, "Ett fel uppstod vid skapande av e-postlista");
        }
    }

    // PUT: api/contact/emaillists/{id}
    [HttpPut("emaillists/{id}")]
    [RequireRole("Admin")]
    public async Task<ActionResult> UpdateEmailList(int id, UpsertEmailListDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var emailList = await _context.EmailLists.FindAsync(id);
            if (emailList == null)
            {
                return NotFound(new { message = "E-postlistan hittades inte." });
            }

            emailList.Name = dto.Name;
            emailList.Description = dto.Description;
            emailList.EmailAddress = dto.EmailAddress;
            emailList.SortOrder = dto.SortOrder;
            emailList.IsActive = dto.IsActive;
            emailList.LastModified = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            
            _logger.LogInformation("E-postlista uppdaterad: {Name} ({Email})", emailList.Name, emailList.EmailAddress);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid uppdatering av e-postlista");
            return StatusCode(500, "Ett fel uppstod vid uppdatering av e-postlista");
        }
    }

    // DELETE: api/contact/emaillists/{id}
    [HttpDelete("emaillists/{id}")]
    [RequireRole("Admin")]
    public async Task<ActionResult> DeleteEmailList(int id)
    {
        try
        {
            var emailList = await _context.EmailLists.FindAsync(id);
            if (emailList == null)
            {
                return NotFound(new { message = "E-postlistan hittades inte." });
            }

            _context.EmailLists.Remove(emailList);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("E-postlista borttagen: {Name} ({Email})", emailList.Name, emailList.EmailAddress);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid borttagning av e-postlista");
            return StatusCode(500, "Ett fel uppstod vid borttagning av e-postlista");
        }
    }

    // PUT: api/contact/emaillists/{id}/toggle-active
    [HttpPut("emaillists/{id}/toggle-active")]
    [RequireRole("Admin")]
    public async Task<ActionResult> ToggleEmailListActive(int id)
    {
        try
        {
            var emailList = await _context.EmailLists.FindAsync(id);
            if (emailList == null)
            {
                return NotFound(new { message = "E-postlistan hittades inte." });
            }

            emailList.IsActive = !emailList.IsActive;
            emailList.LastModified = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("E-postlista {Action}: {Name}", 
                emailList.IsActive ? "aktiverad" : "inaktiverad", emailList.Name);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid ändring av aktivstatus för e-postlista");
            return StatusCode(500, "Ett fel uppstod vid ändring av aktivstatus");
        }
    }
}