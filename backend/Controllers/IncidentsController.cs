using CloudGuard.Api.DTOs;
using CloudGuard.Api.DTOs.Requests;
using CloudGuard.Api.Services.Incidents;
using Microsoft.AspNetCore.Mvc;

namespace CloudGuard.Api.Controllers;

[ApiController]
[Route("api/incidents")]
public class IncidentsController(IIncidentService incidentService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<IncidentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<IncidentDto>>> GetAll(CancellationToken cancellationToken)
    {
        var incidents = await incidentService.GetAllAsync(cancellationToken);
        return Ok(incidents);
    }

    [HttpGet("active")]
    [ProducesResponseType(typeof(IReadOnlyList<IncidentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<IncidentDto>>> GetActive(CancellationToken cancellationToken)
    {
        var incidents = await incidentService.GetActiveAsync(cancellationToken);
        return Ok(incidents);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(IncidentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentDetailDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var incident = await incidentService.GetByIdAsync(id, cancellationToken);
        if (incident is null)
            return NotFound(new { message = $"Incidenti me id {id} nuk u gjet." });

        return Ok(incident);
    }

    [HttpGet("by-service/{serviceId:int}")]
    [ProducesResponseType(typeof(IReadOnlyList<IncidentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<IncidentDto>>> GetByServiceId(
        int serviceId,
        CancellationToken cancellationToken)
    {
        var incidents = await incidentService.GetByServiceIdAsync(serviceId, cancellationToken);
        return Ok(incidents);
    }

    [HttpPost]
    [ProducesResponseType(typeof(IncidentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IncidentDto>> Create(
        [FromBody] CreateIncidentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var incident = await incidentService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = incident.Id }, incident);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(IncidentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentDto>> UpdateStatus(
        int id,
        [FromBody] UpdateIncidentStatusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var incident = await incidentService.UpdateStatusAsync(id, request.Status, cancellationToken);
            if (incident is null)
                return NotFound(new { message = $"Incidenti me id {id} nuk u gjet." });

            return Ok(incident);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:int}/resolve")]
    [ProducesResponseType(typeof(IncidentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentDto>> Resolve(int id, CancellationToken cancellationToken)
    {
        var incident = await incidentService.ResolveAsync(id, cancellationToken);
        if (incident is null)
            return NotFound(new { message = $"Incidenti me id {id} nuk u gjet." });

        return Ok(incident);
    }
}
