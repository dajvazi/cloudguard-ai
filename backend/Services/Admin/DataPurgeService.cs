using CloudGuard.Api.Data;
using CloudGuard.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloudGuard.Api.Services.Admin;

public class DataPurgeService(
    CloudGuardDbContext db,
    ITerraformUploadRepository terraformUploadRepository) : IDataPurgeService
{
    public async Task<PurgeResult> PurgeMetricsAsync(CancellationToken ct = default)
    {
        var count = await db.Metrics.CountAsync(ct);
        await db.Metrics.ExecuteDeleteAsync(ct);
        return new PurgeResult("metrics", count, $"Deleted {count} metrics");
    }

    public async Task<PurgeResult> PurgeAnomaliesAsync(CancellationToken ct = default)
    {
        var count = await db.Anomalies.CountAsync(ct);
        await db.Anomalies.ExecuteDeleteAsync(ct);
        return new PurgeResult("anomalies", count, $"Deleted {count} anomalies");
    }

    public async Task<PurgeResult> PurgeRecoveryActionsAsync(CancellationToken ct = default)
    {
        var count = await db.RecoveryActions.CountAsync(ct);
        await db.RecoveryActions.ExecuteDeleteAsync(ct);
        return new PurgeResult("recovery-actions", count, $"Deleted {count} recovery actions");
    }

    public async Task<PurgeResult> PurgeIncidentsAsync(CancellationToken ct = default)
    {
        var recoveryCount = await db.RecoveryActions.CountAsync(ct);
        if (recoveryCount > 0)
            await db.RecoveryActions.ExecuteDeleteAsync(ct);

        var count = await db.Incidents.CountAsync(ct);
        await db.Incidents.ExecuteDeleteAsync(ct);
        return new PurgeResult(
            "incidents",
            count,
            $"Deleted {count} incidents and {recoveryCount} recovery actions");
    }

    public async Task<PurgeResult> PurgeServicesAsync(CancellationToken ct = default)
    {
        var metrics = await db.Metrics.CountAsync(ct);
        var anomalies = await db.Anomalies.CountAsync(ct);
        var incidents = await db.Incidents.CountAsync(ct);
        var recoveries = await db.RecoveryActions.CountAsync(ct);
        var count = await db.CloudServices.CountAsync(ct);

        await db.RecoveryActions.ExecuteDeleteAsync(ct);
        await db.Incidents.ExecuteDeleteAsync(ct);
        await db.Anomalies.ExecuteDeleteAsync(ct);
        await db.Metrics.ExecuteDeleteAsync(ct);
        await db.CloudServices.ExecuteDeleteAsync(ct);

        return new PurgeResult(
            "services",
            count,
            $"Deleted {count} services (+ {metrics} metrics, {anomalies} anomalies, {incidents} incidents, {recoveries} recovery actions)");
    }

    public async Task<PurgeResult> PurgeResourcesAsync(CancellationToken ct = default)
    {
        var count = await db.Resources.CountAsync(ct);
        await db.Resources.ExecuteDeleteAsync(ct);
        return new PurgeResult("resources", count, $"Deleted {count} resources");
    }

    public async Task<PurgeResult> PurgeTerraformAsync(CancellationToken ct = default)
    {
        var uploads = await db.TerraformUploads.CountAsync(ct);
        var services = await db.CloudServices.CountAsync(s => s.TerraformUploadId != null, ct);
        var resources = await db.Resources.CountAsync(ct);

        await terraformUploadRepository.DeleteAllTerraformDataAsync(ct);

        return new PurgeResult(
            "terraform",
            uploads,
            $"Deleted {uploads} terraform uploads, {services} services, {resources} resources");
    }

    public async Task<PurgeResult> PurgeAwsDataAsync(CancellationToken ct = default)
    {
        var awsServiceIds = await db.CloudServices
            .Where(s => s.SourceKind == "aws")
            .Select(s => s.Id)
            .ToListAsync(ct);

        var count = awsServiceIds.Count;
        if (count == 0)
            return new PurgeResult("aws", 0, "No AWS services to delete");

        var metrics = await db.Metrics
            .CountAsync(m => awsServiceIds.Contains(m.CloudServiceId), ct);

        var awsServices = await db.CloudServices
            .Where(s => s.SourceKind == "aws")
            .ToListAsync(ct);

        db.CloudServices.RemoveRange(awsServices);
        await db.SaveChangesAsync(ct);

        return new PurgeResult(
            "aws",
            count,
            $"Deleted {count} AWS services and {metrics} metrics");
    }
}
