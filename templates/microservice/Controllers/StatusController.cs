using Microsoft.AspNetCore.Mvc;

namespace Microservice.Template.Controllers;

[ApiController]
[Route("api/status")]
public sealed class StatusController : ControllerBase
{
    [HttpGet]
    public ActionResult<StatusResponse> Get()
    {
        return Ok(new StatusResponse(
            Service: HttpContext.RequestServices
                .GetRequiredService<IHostEnvironment>()
                .ApplicationName,
            Environment: HttpContext.RequestServices
                .GetRequiredService<IHostEnvironment>()
                .EnvironmentName,
            Status: "running",
            Timestamp: DateTimeOffset.UtcNow));
    }
}

public sealed record StatusResponse(
    string Service,
    string Environment,
    string Status,
    DateTimeOffset Timestamp);
