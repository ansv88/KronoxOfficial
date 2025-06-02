using MailKit.Net.Smtp;
using MimeKit;

namespace KronoxApi.Services;

// Implementation av IEmailService som skickar e-post via SMTP med MailKit.
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

            // "From": läses från appsettings.json
            message.From.Add(new MailboxAddress(
                _configuration["EmailSettings:FromName"],
                _configuration["EmailSettings:FromEmail"]
            ));

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
            await smtpClient.ConnectAsync(
                _configuration["EmailSettings:SmtpServer"],
                int.Parse(_configuration["EmailSettings:SmtpPort"]),
                bool.Parse(_configuration["EmailSettings:UseSsl"])   // true/false, hittas i appsettings
            );

            smtpClient.AuthenticationMechanisms.Clear(); // För test med smtp4dev, ta bort i produktion

            // Om autentisering krävs, avkommentera nedan:
            //await smtpClient.AuthenticateAsync(
            //    _configuration["EmailSettings:SmtpUser"],
            //    _configuration["EmailSettings:SmtpPass"]
            //); //Utkommenterat för att kunna testa lokalt med smtp4dev

            // Skicka e-post
            await smtpClient.SendAsync(message);
            await smtpClient.DisconnectAsync(true);

            _logger.LogInformation("E-post skickad till {To} med ämne '{Subject}'", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid utskick av e-post till {To} med ämne '{Subject}'", to, subject);
            throw; // Låt felet bubbla upp om det ska hanteras högre upp
        }
    }
}