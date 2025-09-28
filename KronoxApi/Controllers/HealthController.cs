using Microsoft.AspNetCore.Mvc;

namespace KronoxApi.Controllers;

/// <summary>
/// Enkel hälsokontroll för tjänsten (health check).
/// Returnerar status, tidsstämpel och version; loggar fel vid problem.
/// </summary>

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    // Returnerar status för tjänsten.
    [HttpGet]
    [ProducesResponseType(200)]
    public IActionResult Get()
    {
        try
        {
            _logger.LogDebug("Health check anropad: {Time}", DateTime.UtcNow);

            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                version = "1.0"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid health check");
            return StatusCode(500, new
            {
                status = "unhealthy",
                timestamp = DateTime.UtcNow,
                error = ex.Message
            });
        }
    }
}