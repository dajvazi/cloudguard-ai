using CloudGuard.Api.Services.Admin;
using Microsoft.AspNetCore.Mvc;

namespace CloudGuard.Api.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController(IDataPurgeService purgeService) : ControllerBase
{
    [HttpDelete("metrics")]
    [ProducesResponseType(typeof(PurgeResult), StatusCodes.Status200OK)]
    public Task<PurgeResult> DeleteAllMetrics(CancellationToken ct) =>
        purgeService.PurgeMetricsAsync(ct);

    [HttpDelete("anomalies")]
    [ProducesResponseType(typeof(PurgeResult), StatusCodes.Status200OK)]
    public Task<PurgeResult> DeleteAllAnomalies(CancellationToken ct) =>
        purgeService.PurgeAnomaliesAsync(ct);

    [HttpDelete("recovery-actions")]
    [ProducesResponseType(typeof(PurgeResult), StatusCodes.Status200OK)]
    public Task<PurgeResult> DeleteAllRecoveryActions(CancellationToken ct) =>
        purgeService.PurgeRecoveryActionsAsync(ct);

    [HttpDelete("incidents")]
    [ProducesResponseType(typeof(PurgeResult), StatusCodes.Status200OK)]
    public Task<PurgeResult> DeleteAllIncidents(CancellationToken ct) =>
        purgeService.PurgeIncidentsAsync(ct);

    [HttpDelete("services")]
    [ProducesResponseType(typeof(PurgeResult), StatusCodes.Status200OK)]
    public Task<PurgeResult> DeleteAllServices(CancellationToken ct) =>
        purgeService.PurgeServicesAsync(ct);

    [HttpDelete("resources")]
    [ProducesResponseType(typeof(PurgeResult), StatusCodes.Status200OK)]
    public Task<PurgeResult> DeleteAllResources(CancellationToken ct) =>
        purgeService.PurgeResourcesAsync(ct);

    [HttpDelete("terraform")]
    [ProducesResponseType(typeof(PurgeResult), StatusCodes.Status200OK)]
    public Task<PurgeResult> DeleteAllTerraform(CancellationToken ct) =>
        purgeService.PurgeTerraformAsync(ct);

    [HttpDelete("aws")]
    [ProducesResponseType(typeof(PurgeResult), StatusCodes.Status200OK)]
    public Task<PurgeResult> DeleteAllAws(CancellationToken ct) =>
        purgeService.PurgeAwsDataAsync(ct);
}
