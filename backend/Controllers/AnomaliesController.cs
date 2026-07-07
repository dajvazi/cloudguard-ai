using CloudGuard.Api.DTOs;
using CloudGuard.Api.DTOs.Requests;
using CloudGuard.Api.Services.Anomalies;
using Microsoft.AspNetCore.Mvc;

namespace CloudGuard.Api.Controllers;

[ApiController]
[Route("api/anomalies")]
public class AnomaliesController(IAnomalyService anomalyService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AnomalyDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AnomalyDto>>> GetAll(CancellationToken cancellationToken)
    {
        var anomalies = await anomalyService.GetAllAsync(cancellationToken);
        return Ok(anomalies);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AnomalyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnomalyDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var anomaly = await anomalyService.GetByIdAsync(id, cancellationToken);
        if (anomaly is null)
            return NotFound(new { message = $"Anomaly with id {id} was not found." });

        return Ok(anomaly);
    }

    [HttpGet("by-service/{serviceId:int}")]
    [ProducesResponseType(typeof(IReadOnlyList<AnomalyDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AnomalyDto>>> GetByServiceId(
        int serviceId,
        CancellationToken cancellationToken)
    {
        var anomalies = await anomalyService.GetByServiceIdAsync(serviceId, cancellationToken);
        return Ok(anomalies);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AnomalyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AnomalyDto>> Create(
        [FromBody] CreateAnomalyRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var anomaly = await anomalyService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = anomaly.Id }, anomaly);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
