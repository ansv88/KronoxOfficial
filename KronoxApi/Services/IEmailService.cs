namespace KronoxApi.Services;

// Tjänstegränssnitt för att skicka e-post.
public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
}