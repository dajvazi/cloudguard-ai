using CloudGuard.Api.DTOs;
using CloudGuard.Api.Services.CloudServices;
using Microsoft.AspNetCore.Mvc;

namespace CloudGuard.Api.Controllers;

[ApiController]
[Route("api/services")]
public class CloudServicesController(ICloudServiceService cloudServiceService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CloudServiceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CloudServiceDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var services = await cloudServiceService.GetAllAsync(cancellationToken);
        return Ok(services);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CloudServiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CloudServiceDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var service = await cloudServiceService.GetByIdAsync(id, cancellationToken);

        if (service is null)
            return NotFound(new { message = $"Service with id {id} was not found." });

        return Ok(service);
    }

    [HttpGet("by-upload/{uploadId:int}")]
    [ProducesResponseType(typeof(IReadOnlyList<CloudServiceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CloudServiceDto>>> GetByUploadId(
        int uploadId,
        CancellationToken cancellationToken)
    {
        var services = await cloudServiceService.GetByUploadIdAsync(uploadId, cancellationToken);
        return Ok(services);
    }
}
