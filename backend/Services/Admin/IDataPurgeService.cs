namespace CloudGuard.Api.Services.Admin;

public record PurgeResult(string Module, int DeletedCount, string Message);

public interface IDataPurgeService
{
    Task<PurgeResult> PurgeMetricsAsync(CancellationToken ct = default);
    Task<PurgeResult> PurgeAnomaliesAsync(CancellationToken ct = default);
    Task<PurgeResult> PurgeRecoveryActionsAsync(CancellationToken ct = default);
    Task<PurgeResult> PurgeIncidentsAsync(CancellationToken ct = default);
    Task<PurgeResult> PurgeServicesAsync(CancellationToken ct = default);
    Task<PurgeResult> PurgeResourcesAsync(CancellationToken ct = default);
    Task<PurgeResult> PurgeTerraformAsync(CancellationToken ct = default);
    Task<PurgeResult> PurgeAwsDataAsync(CancellationToken ct = default);
}
