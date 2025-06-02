using Microsoft.AspNetCore.Mvc;

namespace KronoxApi.Controllers;

// API-kontroller f�r h�lsokontroll av tj�nsten.
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }


    // Returnerar status f�r tj�nsten.
    [HttpGet]
    [ProducesResponseType(200)]
    public IActionResult Get()
    {
        try
        {
            _logger.LogInformation("Health check anropad: {Time}", DateTime.UtcNow);

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