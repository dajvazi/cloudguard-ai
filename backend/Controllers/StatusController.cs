using Microsoft.AspNetCore.Mvc;

namespace CloudGuard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            message = "CloudGuard API është online!",
            version = "1.0.0",
            timestamp = DateTime.UtcNow
        });
    }
}
