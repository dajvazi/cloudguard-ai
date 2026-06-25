using CloudGuard.Api.Services.AWS;
using Microsoft.AspNetCore.Mvc;

namespace CloudGuard.Api.Controllers;

[ApiController]
[Route("api/aws")]
public class AwsController(IAwsCloudWatchService awsService) : ControllerBase
{
    [HttpGet("test-connection")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> TestConnection(CancellationToken ct)
    {
        var connected = await awsService.TestConnectionAsync(ct);
        return Ok(new { connected, message = connected ? "AWS connection successful" : "AWS connection failed" });
    }

    [HttpPost("import")]
    [ProducesResponseType(typeof(AwsImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AwsImportResult>> Import(
        [FromBody] AwsImportRequest request,
        CancellationToken ct)
    {
        var result = await awsService.ImportCloudWatchDataAsync(request, ct);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}
