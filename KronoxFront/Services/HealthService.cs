using System.Text.Json;

namespace KronoxFront.Services;

// Service f�r att kontrollera h�lsostatus hos API:et.
public class HealthService
{
    private readonly HttpClient _http;
    private readonly ILogger<HealthService> _logger;
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(10);

    public HealthService(HttpClient http, ILogger<HealthService> logger)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> IsApiHealthyAsync(CancellationToken cancellationToken = default)
    {
        using CancellationTokenSource timeoutCts = new CancellationTokenSource(_defaultTimeout);
        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

        try
        {
            _logger.LogInformation("Kontrollerar API:ets h�lsostatus");

            var response = await _http.GetAsync("api/health", linkedCts.Token);

            if (response.IsSuccessStatusCode)
            {
                var healthData = await response.Content.ReadFromJsonAsync<ApiHealth>(
                    cancellationToken: linkedCts.Token);

                if (healthData == null)
                {
                    _logger.LogWarning("API returnerade ett tomt h�lsosvar");
                    return false;
                }

                _logger.LogInformation("API �r tillg�ngligt. Status: {Status}, Tidsst�mpel: {Timestamp}, Version: {Version}",
                    healthData.Status, healthData.Timestamp, healthData.Version);

                // Kontrollera att Status faktiskt �r "Healthy"
                return healthData.Status?.Equals("Healthy", StringComparison.OrdinalIgnoreCase) == true;
            }

            _logger.LogWarning("API svarade med statuskod {StatusCode}", response.StatusCode);
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Kunde inte n� API:s health endpoint");
            return false;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Kunde inte tolka API:s h�lsostatussvar");
            return false;
        }
        catch (OperationCanceledException ex)
        {
            if (timeoutCts.Token.IsCancellationRequested)
            {
                _logger.LogWarning("Timeout vid kontroll av API:ets h�lsostatus");
            }
            else
            {
                _logger.LogError(ex, "Kontrollen av API:ets h�lsostatus avbr�ts");
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ov�ntat fel vid kontroll av API:ets h�lsostatus");
            return false;
        }
    }

    private class ApiHealth
    {
        public string Status { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Version { get; set; }
    }
}