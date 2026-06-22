using CloudGuard.Api.DTOs;
using CloudGuard.Api.DTOs.Requests;
using CloudGuard.Api.Services.Metrics;
using Microsoft.AspNetCore.Mvc;

namespace CloudGuard.Api.Controllers;

[ApiController]
[Route("api/metrics")]
public class MetricsController(IMetricService metricService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MetricDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MetricDto>>> GetAll(CancellationToken cancellationToken)
    {
        var metrics = await metricService.GetAllAsync(cancellationToken);
        return Ok(metrics);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(MetricDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MetricDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var metric = await metricService.GetByIdAsync(id, cancellationToken);
        if (metric is null)
            return NotFound(new { message = $"Metrika me id {id} nuk u gjet." });

        return Ok(metric);
    }

    [HttpGet("by-service/{serviceId:int}")]
    [ProducesResponseType(typeof(IReadOnlyList<MetricDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MetricDto>>> GetByServiceId(
        int serviceId,
        CancellationToken cancellationToken)
    {
        var metrics = await metricService.GetByServiceIdAsync(serviceId, cancellationToken);
        return Ok(metrics);
    }

    [HttpPost]
    [ProducesResponseType(typeof(MetricDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MetricDto>> Create(
        [FromBody] CreateMetricRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var metric = await metricService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = metric.Id }, metric);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
