namespace KronoxApi.Services;

/// <summary>
/// Tjänstegränssnitt för att skicka e‑post.
/// </summary>
public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
}