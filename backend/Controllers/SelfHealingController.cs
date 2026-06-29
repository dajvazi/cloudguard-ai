using CloudGuard.Api.Services.AI;
using Microsoft.AspNetCore.Mvc;

namespace CloudGuard.Api.Controllers;

[ApiController]
[Route("api/self-healing")]
public class SelfHealingController(ISelfHealingOrchestrator orchestrator) : ControllerBase
{
    private static ActionResult<SelfHealingResult> ToResponse(SelfHealingResult result)
    {
        // Pipeline ran (incident/recovery created) — return body even if SSM failed
        if (result.IncidentId is not null || result.RecoveryActionId is not null)
            return new OkObjectResult(result);

        if (!result.Success)
            return new BadRequestObjectResult(result);

        return new OkObjectResult(result);
    }

    [HttpPost("trigger/{serviceId:int}")]
    [ProducesResponseType(typeof(SelfHealingResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SelfHealingResult>> TriggerByService(
        int serviceId,
        CancellationToken cancellationToken)
    {
        var result = await orchestrator.TriggerAsync(serviceId, cancellationToken);
        return ToResponse(result);
    }

    [HttpPost("trigger/anomaly/{anomalyId:int}")]
    [ProducesResponseType(typeof(SelfHealingResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SelfHealingResult>> TriggerByAnomaly(
        int anomalyId,
        CancellationToken cancellationToken)
    {
        var result = await orchestrator.TriggerFromAnomalyAsync(anomalyId, cancellationToken);
        return ToResponse(result);
    }

    [HttpPost("trigger/incident/{incidentId:int}")]
    [ProducesResponseType(typeof(SelfHealingResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SelfHealingResult>> TriggerByIncident(
        int incidentId,
        CancellationToken cancellationToken)
    {
        var result = await orchestrator.TriggerFromIncidentAsync(incidentId, cancellationToken);
        return ToResponse(result);
    }

    [HttpGet("analyze/{serviceId:int}")]
    [ProducesResponseType(typeof(HealingAnalysis), StatusCodes.Status200OK)]
    public async Task<ActionResult<HealingAnalysis>> Analyze(
        int serviceId,
        CancellationToken cancellationToken)
    {
        var analysis = await orchestrator.AnalyzeAsync(serviceId, cancellationToken);
        return Ok(analysis);
    }

    [HttpPost("execute/{serviceId:int}/{runbookId}")]
    [ProducesResponseType(typeof(SelfHealingResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SelfHealingResult>> ExecuteRunbook(
        int serviceId,
        string runbookId,
        CancellationToken cancellationToken)
    {
        var result = await orchestrator.ExecuteRunbookAsync(serviceId, runbookId, cancellationToken);
        return ToResponse(result);
    }
}
