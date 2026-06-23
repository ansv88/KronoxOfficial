namespace KronoxApi.Services;

/// <summary>
/// Tjänstegränssnitt för att verifiera Google reCAPTCHA v3-tokens.
/// </summary>
public interface IRecaptchaService
{
    Task<RecaptchaResult> VerifyAsync(string? token, string expectedAction, CancellationToken ct = default);
}

/// <summary>
/// Resultat av en reCAPTCHA-verifiering: utfall, score och eventuellt felmeddelande.
/// </summary>
public record RecaptchaResult(bool Success, double Score, string? Error)
{
    public static RecaptchaResult Ok(double score) => new(true, score, null);
    public static RecaptchaResult Fail(string error) => new(false, 0, error);
}