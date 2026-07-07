using CloudGuard.Api.DTOs;
using CloudGuard.Api.DTOs.Requests;
using CloudGuard.Api.Services.Resources;
using Microsoft.AspNetCore.Mvc;

namespace CloudGuard.Api.Controllers;

[ApiController]
[Route("api/resources")]
public class ResourcesController(IResourceService resourceService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ResourceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ResourceDto>>> GetAll(CancellationToken cancellationToken)
    {
        var resources = await resourceService.GetAllAsync(cancellationToken);
        return Ok(resources);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ResourceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResourceDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var resource = await resourceService.GetByIdAsync(id, cancellationToken);
        if (resource is null)
            return NotFound(new { message = $"Resource with id {id} was not found." });

        return Ok(resource);
    }

    [HttpGet("by-source")]
    [ProducesResponseType(typeof(IReadOnlyList<ResourceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ResourceDto>>> GetBySource(
        [FromQuery] string source,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(source))
            return BadRequest(new { message = "The 'source' parameter is required." });

        var resources = await resourceService.GetBySourceAsync(source, cancellationToken);
        return Ok(resources);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ResourceDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ResourceDto>> Create(
        [FromBody] CreateResourceRequest request,
        CancellationToken cancellationToken)
    {
        var resource = await resourceService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = resource.Id }, resource);
    }
}
