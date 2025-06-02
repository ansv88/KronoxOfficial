using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace KronoxFront.Services
{
    public class HealthService
    {
        private readonly HttpClient _http;
        private readonly ILogger<HealthService> _logger;

        public HealthService(HttpClient http, ILogger<HealthService> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<bool> IsApiHealthyAsync()
        {
            try
            {
                _logger.LogInformation("Kontrollerar API:ets hälsostatus");
                var response = await _http.GetAsync("api/health");
                
                if (response.IsSuccessStatusCode)
                {
                    var healthData = await response.Content.ReadFromJsonAsync<ApiHealth>();
                    _logger.LogInformation("API är tillgängligt. Status: {Status}, Tidsstämpel: {Timestamp}", 
                        healthData?.Status, healthData?.Timestamp);
                    return true;
                }
                
                _logger.LogWarning("API svarade med statuskod {StatusCode}", response.StatusCode);
                return false;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Kunde inte nå API:s health endpoint");
                return false;
            }
        }
        
        private class ApiHealth
        {
            public string Status { get; set; }
            public DateTime Timestamp { get; set; }
            public string Version { get; set; }
        }
    }
}
