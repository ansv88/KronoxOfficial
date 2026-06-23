using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace KronoxApi.Services;

/// <summary>
/// Implementation av IRecaptchaService som verifierar reCAPTCHA v3-tokens mot
/// Googles verifierings-API. Läser inställningar från Recaptcha i konfigurationen.
/// </summary>
public class RecaptchaService : IRecaptchaService
{
    private const string VerifyUrl = "https://www.google.com/recaptcha/api/siteverify";

    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<RecaptchaService> _logger;

    public RecaptchaService(HttpClient http, IConfiguration config, ILogger<RecaptchaService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task<RecaptchaResult> VerifyAsync(string? token, string expectedAction, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return RecaptchaResult.Fail("reCAPTCHA-token saknas.");

        var secret = _config["Recaptcha:SecretKey"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            _logger.LogError("Recaptcha:SecretKey saknas i konfigurationen.");
            return RecaptchaResult.Fail("Serverkonfiguration för reCAPTCHA saknas.");
        }

        var minScore = _config.GetValue<double?>("Recaptcha:MinimumScore") ?? 0.5;

        try
        {
            using var response = await _http.PostAsync(VerifyUrl, new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["secret"] = secret,
                    ["response"] = token!
                }), ct);

            var payload = await response.Content.ReadFromJsonAsync<RecaptchaApiResponse>(cancellationToken: ct);

            if (payload is null || !payload.Success)
            {
                _logger.LogWarning("reCAPTCHA misslyckades. Felkoder: {Codes}",
                    payload?.ErrorCodes is null ? "okänt" : string.Join(", ", payload.ErrorCodes));
                return RecaptchaResult.Fail("reCAPTCHA-verifiering misslyckades.");
            }

            // Kontrollera att rätt formulär (action) används
            if (!string.IsNullOrEmpty(expectedAction) &&
                !string.Equals(payload.Action, expectedAction, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("reCAPTCHA action matchade inte. Förväntat: {Expected}, fick: {Actual}",
                    expectedAction, payload.Action);
                return RecaptchaResult.Fail("Ogiltig reCAPTCHA-action.");
            }

            // Score nära 1.0 = troligen människa, nära 0.0 = troligen bot
            if (payload.Score < minScore)
            {
                _logger.LogWarning("reCAPTCHA-score för låg: {Score} (min {Min})", payload.Score, minScore);
                return RecaptchaResult.Fail("Misstänkt automatiserad trafik. Försök igen.");
            }

            return RecaptchaResult.Ok(payload.Score);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tekniskt fel vid verifiering av reCAPTCHA.");
            return RecaptchaResult.Fail("Tekniskt fel vid reCAPTCHA-verifiering.");
        }
    }

    private sealed class RecaptchaApiResponse
    {
        [JsonPropertyName("success")] public bool Success { get; set; }
        [JsonPropertyName("score")] public double Score { get; set; }
        [JsonPropertyName("action")] public string? Action { get; set; }
        [JsonPropertyName("challenge_ts")] public string? ChallengeTimestamp { get; set; }
        [JsonPropertyName("hostname")] public string? Hostname { get; set; }
        [JsonPropertyName("error-codes")] public string[]? ErrorCodes { get; set; }
    }
}