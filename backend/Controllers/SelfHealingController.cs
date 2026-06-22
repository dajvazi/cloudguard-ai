using CloudGuard.Api.Services.AI;
using Microsoft.AspNetCore.Mvc;

namespace CloudGuard.Api.Controllers;

[ApiController]
[Route("api/self-healing")]
public class SelfHealingController(ISelfHealingOrchestrator orchestrator) : ControllerBase
{
    [HttpPost("trigger/{serviceId:int}")]
    [ProducesResponseType(typeof(SelfHealingResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SelfHealingResult>> TriggerByService(
        int serviceId,
        CancellationToken cancellationToken)
    {
        var result = await orchestrator.TriggerAsync(serviceId, cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("trigger/anomaly/{anomalyId:int}")]
    [ProducesResponseType(typeof(SelfHealingResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SelfHealingResult>> TriggerByAnomaly(
        int anomalyId,
        CancellationToken cancellationToken)
    {
        var result = await orchestrator.TriggerFromAnomalyAsync(anomalyId, cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}
