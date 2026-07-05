using MailKit.Net.Smtp;
using MimeKit;

namespace KronoxApi.Services;

/// <summary>
/// Implementation av IEmailService som skickar e‑post via SMTP med MailKit.
/// Läser inställningar från EmailSettings i konfigurationen.
/// </summary>
public class MailKitEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MailKitEmailService> _logger;

    public MailKitEmailService(IConfiguration configuration, ILogger<MailKitEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    // Skickar ett e-postmeddelande via SMTP.
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            // Skapa ett MimeMessage-objekt
            var message = new MimeMessage();

            // Läs och validera e‑postinställningar från konfigurationen
            var fromName = _configuration["EmailSettings:FromName"];
            var fromEmail = _configuration["EmailSettings:FromEmail"]
                ?? throw new InvalidOperationException("EmailSettings:FromEmail saknas i konfigurationen.");
            var smtpServer = _configuration["EmailSettings:SmtpServer"]
                ?? throw new InvalidOperationException("EmailSettings:SmtpServer saknas i konfigurationen.");
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]
                ?? throw new InvalidOperationException("EmailSettings:SmtpPort saknas i konfigurationen."));
            var useSsl = bool.Parse(_configuration["EmailSettings:UseSsl"]
                ?? throw new InvalidOperationException("EmailSettings:UseSsl saknas i konfigurationen."));

            // "From": läses från appsettings.json
            message.From.Add(new MailboxAddress(fromName, fromEmail));

            // "To": den adress vi skickar till
            message.To.Add(MailboxAddress.Parse(to));

            // Ämne och meddelande
            message.Subject = subject;
            message.Body = new TextPart("plain")
            {
                Text = body
            };

            // Anslut till SMTP-server med info från konfiguration
            using var smtpClient = new SmtpClient();
            await smtpClient.ConnectAsync(smtpServer, smtpPort, useSsl);   // true/false, hittas i appsettings

            smtpClient.AuthenticationMechanisms.Clear(); // För test med smtp4dev, ta bort i produktion

            // Om autentisering krävs, avkommentera nedan:
            //await smtpClient.AuthenticateAsync(
            //    _configuration["EmailSettings:SmtpUser"],
            //    _configuration["EmailSettings:SmtpPass"]
            //); //Utkommenterat för att kunna testa lokalt med smtp4dev

            // Skicka e-post
            await smtpClient.SendAsync(message);
            await smtpClient.DisconnectAsync(true);

            _logger.LogInformation("E‑post skickad till {To} med ämne '{Subject}'", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid utskick av e‑post till {To} med ämne '{Subject}'", to, subject);
            throw; // Låt felet bubbla upp om det ska hanteras högre upp
        }
    }
}