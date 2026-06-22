using CloudGuard.Api.Constants;
using CloudGuard.Api.Data;
using CloudGuard.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudGuard.Api.Services.AI;

public class SelfHealingOrchestrator(
    CloudGuardDbContext db,
    IAiAnalysisService aiService,
    ILogger<SelfHealingOrchestrator> logger) : ISelfHealingOrchestrator
{
    public async Task<SelfHealingResult> TriggerAsync(
        int serviceId,
        CancellationToken cancellationToken = default)
    {
        var service = await db.CloudServices.FirstOrDefaultAsync(s => s.Id == serviceId, cancellationToken);
        if (service is null)
            return new SelfHealingResult(false, $"Service {serviceId} not found", null, null, null, null);

        var latestAnomaly = await db.Anomalies
            .Where(a => a.CloudServiceId == serviceId)
            .OrderByDescending(a => a.DetectedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestAnomaly is null)
            return new SelfHealingResult(false, $"No anomalies found for service {service.Name}", null, null, null, null);

        return await ExecutePipelineAsync(service, latestAnomaly, cancellationToken);
    }

    public async Task<SelfHealingResult> TriggerFromAnomalyAsync(
        int anomalyId,
        CancellationToken cancellationToken = default)
    {
        var anomaly = await db.Anomalies
            .Include(a => a.CloudService)
            .FirstOrDefaultAsync(a => a.Id == anomalyId, cancellationToken);

        if (anomaly is null)
            return new SelfHealingResult(false, $"Anomaly {anomalyId} not found", null, null, null, null);

        return await ExecutePipelineAsync(anomaly.CloudService, anomaly, cancellationToken);
    }

    private async Task<SelfHealingResult> ExecutePipelineAsync(
        CloudService service,
        Anomaly anomaly,
        CancellationToken ct)
    {
        logger.LogInformation(
            "Self-healing pipeline triggered for {Service} (anomaly: {AnomalyType})",
            service.Name, anomaly.AnomalyType);

        // Step 1: AI Analysis
        var analysis = await aiService.AnalyzeIncidentAsync(
            service.Name,
            service.Type,
            anomaly.AnomalyType ?? "Unknown",
            anomaly.Description ?? "",
            ct);

        logger.LogInformation("AI analysis complete: {RootCause}", analysis.RootCause);

        // Step 2: Create Incident
        var incident = new Incident
        {
            CloudServiceId = service.Id,
            Title = $"[Auto] {anomaly.AnomalyType} on {service.Name}",
            Severity = analysis.Severity,
            Status = IncidentStatus.Investigating,
            RootCause = analysis.RootCause,
            CreatedAt = DateTime.UtcNow,
        };

        db.Incidents.Add(incident);
        await db.SaveChangesAsync(ct);

        // Step 3: Create Recovery Action
        var recoveryAction = new RecoveryAction
        {
            IncidentId = incident.Id,
            ActionType = analysis.ActionType,
            ActionStatus = RecoveryActionStatus.InProgress,
            Description = analysis.RecommendedAction,
            ExecutedAt = DateTime.UtcNow,
        };

        db.RecoveryActions.Add(recoveryAction);

        // Step 4: Update service status
        service.Status = ServiceStatus.Recovering;
        incident.Status = IncidentStatus.Mitigating;

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Self-healing executing: {ActionType} for incident {IncidentId}",
            analysis.ActionType, incident.Id);

        // Step 5: Simulate execution (in real system, this would be async)
        await Task.Delay(TimeSpan.FromSeconds(2), ct);

        recoveryAction.ActionStatus = RecoveryActionStatus.Completed;
        incident.Status = IncidentStatus.Resolved;
        incident.ResolvedAt = DateTime.UtcNow;
        service.Status = ServiceStatus.Healthy;

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Self-healing completed: {Service} restored to healthy",
            service.Name);

        return new SelfHealingResult(
            Success: true,
            Message: $"Self-healing completed: {service.Name} restored to healthy state",
            AnomalyId: anomaly.Id,
            IncidentId: incident.Id,
            RecoveryActionId: recoveryAction.Id,
            AiAnalysis: analysis);
    }
}
