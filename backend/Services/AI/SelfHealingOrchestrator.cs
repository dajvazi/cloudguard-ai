using CloudGuard.Api.Constants;
using CloudGuard.Api.Data;
using CloudGuard.Api.Models;
using CloudGuard.Api.Services.AWS;
using CloudGuard.Api.Services.AWS.Runbooks;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace CloudGuard.Api.Services.AI;

public partial class SelfHealingOrchestrator(
    CloudGuardDbContext db,
    IAiAnalysisService aiService,
    IRunbookService runbookService,
    IAwsSsmService ssmService,
    IConfiguration configuration,
    ILogger<SelfHealingOrchestrator> logger) : ISelfHealingOrchestrator
{
    [GeneratedRegex(@"^i-[0-9a-f]+$", RegexOptions.IgnoreCase)]
    private static partial Regex Ec2InstanceIdPattern();

    public async Task<SelfHealingResult> TriggerAsync(
        int serviceId,
        CancellationToken cancellationToken = default)
    {
        var service = await db.CloudServices.FirstOrDefaultAsync(s => s.Id == serviceId, cancellationToken);
        if (service is null)
            return Failed($"Service {serviceId} not found");

        var latestAnomaly = await db.Anomalies
            .Where(a => a.CloudServiceId == serviceId)
            .OrderByDescending(a => a.DetectedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestAnomaly is null)
            return Failed($"No anomalies found for service {service.Name}");

        var existingIncident = await FindOpenIncidentAsync(service.Id, cancellationToken);
        if (existingIncident is null && await HasResolvedIncidentsAsync(service.Id, cancellationToken))
            return Failed($"Incidents for {service.Name} are already resolved. Nothing to heal.");

        return await ExecutePipelineAsync(service, latestAnomaly, existingIncident, cancellationToken);
    }

    public async Task<SelfHealingResult> TriggerFromAnomalyAsync(
        int anomalyId,
        CancellationToken cancellationToken = default)
    {
        var anomaly = await db.Anomalies
            .Include(a => a.CloudService)
            .FirstOrDefaultAsync(a => a.Id == anomalyId, cancellationToken);

        if (anomaly is null)
            return Failed($"Anomaly {anomalyId} not found");

        var existingIncident = await FindOpenIncidentAsync(anomaly.CloudServiceId, cancellationToken);
        if (existingIncident is null && await HasResolvedIncidentsAsync(anomaly.CloudServiceId, cancellationToken))
            return Failed($"Incidents for {anomaly.CloudService.Name} are already resolved. Nothing to heal.");

        return await ExecutePipelineAsync(anomaly.CloudService, anomaly, existingIncident, cancellationToken);
    }

    public async Task<SelfHealingResult> TriggerFromIncidentAsync(
        int incidentId,
        CancellationToken cancellationToken = default)
    {
        var incident = await db.Incidents
            .Include(i => i.CloudService)
            .FirstOrDefaultAsync(i => i.Id == incidentId, cancellationToken);

        if (incident is null)
            return Failed($"Incident {incidentId} not found");

        if (incident.Status == IncidentStatus.Resolved)
            return Failed($"Incident {incidentId} is already resolved");

        var inProgress = await db.RecoveryActions.AnyAsync(
            r => r.IncidentId == incidentId &&
                 r.ActionStatus == RecoveryActionStatus.InProgress,
            cancellationToken);

        if (inProgress)
            return Failed($"Recovery already in progress for incident {incidentId}");

        var anomaly = await db.Anomalies
            .Where(a => a.CloudServiceId == incident.CloudServiceId)
            .OrderByDescending(a => a.DetectedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (anomaly is null)
            return Failed($"No anomalies found for service {incident.CloudService.Name}");

        return await ExecutePipelineAsync(incident.CloudService, anomaly, incident, cancellationToken);
    }

    private async Task<Incident?> FindOpenIncidentAsync(int serviceId, CancellationToken ct) =>
        await db.Incidents
            .Where(i =>
                i.CloudServiceId == serviceId &&
                i.Status != IncidentStatus.Resolved)
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefaultAsync(ct);

    private async Task<bool> HasResolvedIncidentsAsync(int serviceId, CancellationToken ct) =>
        await db.Incidents.AnyAsync(
            i => i.CloudServiceId == serviceId && i.Status == IncidentStatus.Resolved,
            ct);

    private async Task ResolveOpenIncidentsForServiceAsync(int serviceId, CancellationToken ct)
    {
        var open = await db.Incidents
            .Where(i => i.CloudServiceId == serviceId && i.Status != IncidentStatus.Resolved)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        foreach (var incident in open)
        {
            incident.Status = IncidentStatus.Resolved;
            incident.ResolvedAt = now;
        }
    }

    private async Task<SelfHealingResult> ExecutePipelineAsync(
        CloudService service,
        Anomaly anomaly,
        Incident? existingIncident,
        CancellationToken ct)
    {
        logger.LogInformation(
            "Self-healing pipeline triggered for {Service} (anomaly: {AnomalyType})",
            service.Name, anomaly.AnomalyType);

        var analysis = await aiService.AnalyzeIncidentAsync(
            service.Name,
            service.Type,
            anomaly.AnomalyType ?? "Unknown",
            anomaly.Description ?? "",
            ct);

        Incident incident;
        if (existingIncident is not null)
        {
            incident = existingIncident;
            incident.Title = $"[Healing] {anomaly.AnomalyType} on {service.Name}";
            incident.Severity = analysis.Severity;
            incident.Status = IncidentStatus.Investigating;
            incident.RootCause = analysis.RootCause;
        }
        else
        {
            incident = new Incident
            {
                CloudServiceId = service.Id,
                Title = $"[Auto] {anomaly.AnomalyType} on {service.Name}",
                Severity = analysis.Severity,
                Status = IncidentStatus.Investigating,
                RootCause = analysis.RootCause,
                CreatedAt = DateTime.UtcNow,
            };
            db.Incidents.Add(incident);
        }

        await db.SaveChangesAsync(ct);

        var runbook = runbookService.Resolve(
            anomaly.AnomalyType ?? "",
            analysis.ActionType,
            service.SourceKind);

        var recoveryAction = new RecoveryAction
        {
            IncidentId = incident.Id,
            ActionType = runbook?.Name ?? analysis.ActionType,
            ActionStatus = RecoveryActionStatus.InProgress,
            Description = analysis.RecommendedAction,
            ExecutedAt = DateTime.UtcNow,
        };

        db.RecoveryActions.Add(recoveryAction);
        service.Status = ServiceStatus.Recovering;
        incident.Status = IncidentStatus.Mitigating;
        await db.SaveChangesAsync(ct);

        SsmExecutionResult? ssmResult = null;

        if (runbook is not null && ssmService.IsEnabled)
        {
            var instanceId = ResolveInstanceId(service);
            if (instanceId is not null)
            {
                logger.LogInformation(
                    "Executing runbook {RunbookId} via SSM on {InstanceId}",
                    runbook.Id, instanceId);

                ssmResult = await ssmService.ExecuteRunbookAsync(instanceId, runbook.Commands, ct);
                recoveryAction.Description =
                    $"{analysis.RecommendedAction}\n\n[SSM {ssmResult.CommandId}]\n{ssmResult.Output}";

                if (!string.IsNullOrWhiteSpace(ssmResult.Error))
                    recoveryAction.Description += $"\n\nSTDERR:\n{ssmResult.Error}";
            }
            else
            {
                logger.LogWarning("No EC2 instance ID for service {ServiceName}", service.Name);
            }
        }

        if (ssmResult is null)
        {
            logger.LogInformation("SSM not used — simulating recovery for {Service}", service.Name);
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
            recoveryAction.Description += "\n\n[Simulated — SSM disabled or non-AWS service]";
            recoveryAction.ActionStatus = RecoveryActionStatus.Completed;
            await ResolveOpenIncidentsForServiceAsync(service.Id, ct);
            service.Status = ServiceStatus.Healthy;

            await db.SaveChangesAsync(ct);

            return new SelfHealingResult(
                Success: true,
                Message: $"Self-healing completed (simulated): {service.Name}",
                AnomalyId: anomaly.Id,
                IncidentId: incident.Id,
                RecoveryActionId: recoveryAction.Id,
                AiAnalysis: analysis,
                RunbookId: runbook?.Id,
                ExecutedViaSsm: false);
        }

        if (ssmResult.Success)
        {
            recoveryAction.ActionStatus = RecoveryActionStatus.Completed;
            await ResolveOpenIncidentsForServiceAsync(service.Id, ct);
            service.Status = ServiceStatus.Healthy;
        }
        else
        {
            recoveryAction.ActionStatus = RecoveryActionStatus.Failed;
            incident.Status = IncidentStatus.Open;
            service.Status = ServiceStatus.Critical;
        }

        await db.SaveChangesAsync(ct);

        return new SelfHealingResult(
            Success: ssmResult.Success,
            Message: ssmResult.Success
                ? $"SSM runbook '{runbook!.Id}' executed on {service.Name}"
                : $"SSM runbook failed: {ssmResult.Error ?? ssmResult.Status}",
            AnomalyId: anomaly.Id,
            IncidentId: incident.Id,
            RecoveryActionId: recoveryAction.Id,
            AiAnalysis: analysis,
            RunbookId: runbook!.Id,
            SsmCommandId: ssmResult.CommandId,
            ExecutionOutput: ssmResult.Success
                ? ssmResult.Output
                : (ssmResult.Error ?? ssmResult.Output),
            ExecutedViaSsm: true);
    }

    public async Task<HealingAnalysis> AnalyzeAsync(
        int serviceId,
        CancellationToken cancellationToken = default)
    {
        var service = await db.CloudServices.FirstOrDefaultAsync(s => s.Id == serviceId, cancellationToken);
        if (service is null)
            return new HealingAnalysis(false, "Unknown", null, null, []);

        var openIncident = await FindOpenIncidentAsync(serviceId, cancellationToken);
        if (openIncident is null)
            return new HealingAnalysis(false, service.Name, null, null, []);

        var latestAnomaly = await db.Anomalies
            .Where(a => a.CloudServiceId == serviceId)
            .OrderByDescending(a => a.DetectedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestAnomaly is null)
            return new HealingAnalysis(false, service.Name, null, null, []);

        var analysis = await aiService.AnalyzeIncidentAsync(
            service.Name,
            service.Type,
            latestAnomaly.AnomalyType ?? "Unknown",
            latestAnomaly.Description ?? "",
            cancellationToken);

        var allRunbooks = runbookService.GetAll();
        var recommended = runbookService.Resolve(
            latestAnomaly.AnomalyType ?? "",
            analysis.ActionType,
            service.SourceKind);

        var options = allRunbooks.Select(rb => new HealingOption(
            RunbookId: rb.Id,
            Name: rb.Name,
            Description: rb.Description,
            Effect: GetRunbookEffect(rb.Id, service.Name),
            Recommended: rb.Id == recommended?.Id
        )).ToList();

        return new HealingAnalysis(
            Success: true,
            ServiceName: service.Name,
            AnomalyType: latestAnomaly.AnomalyType,
            AiAnalysis: analysis,
            Options: options);
    }

    public async Task<SelfHealingResult> ExecuteRunbookAsync(
        int serviceId,
        string runbookId,
        int? incidentId = null,
        CancellationToken cancellationToken = default)
    {
        var service = await db.CloudServices.FirstOrDefaultAsync(s => s.Id == serviceId, cancellationToken);
        if (service is null)
            return Failed($"Service {serviceId} not found");

        var runbook = runbookService.GetById(runbookId);
        if (runbook is null)
            return Failed($"Runbook '{runbookId}' not found");

        Incident? incident = null;
        if (incidentId is not null)
        {
            incident = await db.Incidents.FirstOrDefaultAsync(
                i => i.Id == incidentId && i.CloudServiceId == serviceId,
                cancellationToken);

            if (incident is null)
                return Failed($"Incident {incidentId} not found for this service");

            if (incident.Status == IncidentStatus.Resolved)
                return Failed($"Incident {incidentId} is already resolved");
        }
        else
        {
            incident = await FindOpenIncidentAsync(service.Id, cancellationToken);
            if (incident is null)
            {
                if (await HasResolvedIncidentsAsync(service.Id, cancellationToken))
                    return Failed($"Incidents for {service.Name} are already resolved. Nothing to heal.");

                return Failed($"No open incident found for {service.Name}");
            }
        }

        var latestAnomaly = await db.Anomalies
            .Where(a => a.CloudServiceId == serviceId)
            .OrderByDescending(a => a.DetectedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var analysis = await aiService.AnalyzeIncidentAsync(
            service.Name,
            service.Type,
            latestAnomaly?.AnomalyType ?? "Unknown",
            latestAnomaly?.Description ?? "",
            cancellationToken);

        incident.Status = IncidentStatus.Investigating;
        incident.RootCause = analysis.RootCause;
        await db.SaveChangesAsync(cancellationToken);

        var recoveryAction = new RecoveryAction
        {
            IncidentId = incident.Id,
            ActionType = runbook.Name,
            ActionStatus = RecoveryActionStatus.InProgress,
            Description = analysis.RecommendedAction,
            ExecutedAt = DateTime.UtcNow,
        };

        db.RecoveryActions.Add(recoveryAction);
        service.Status = ServiceStatus.Recovering;
        incident.Status = IncidentStatus.Mitigating;
        await db.SaveChangesAsync(cancellationToken);

        var instanceId = ResolveInstanceId(service);

        if (instanceId is not null && ssmService.IsEnabled)
        {
            var ssmResult = await ssmService.ExecuteRunbookAsync(instanceId, runbook.Commands, cancellationToken);
            recoveryAction.Description =
                $"{analysis.RecommendedAction}\n\n[SSM {ssmResult.CommandId}]\n{ssmResult.Output}";

            if (!string.IsNullOrWhiteSpace(ssmResult.Error))
                recoveryAction.Description += $"\n\nSTDERR:\n{ssmResult.Error}";

            if (ssmResult.Success)
            {
                recoveryAction.ActionStatus = RecoveryActionStatus.Completed;
                await ResolveOpenIncidentsForServiceAsync(service.Id, cancellationToken);
                service.Status = ServiceStatus.Healthy;
            }
            else
            {
                recoveryAction.ActionStatus = RecoveryActionStatus.Failed;
                incident.Status = IncidentStatus.Open;
                service.Status = ServiceStatus.Critical;
            }

            await db.SaveChangesAsync(cancellationToken);

            return new SelfHealingResult(
                Success: ssmResult.Success,
                Message: ssmResult.Success
                    ? $"Runbook '{runbook.Id}' executed on {service.Name}"
                    : $"Runbook failed: {ssmResult.Error ?? ssmResult.Status}",
                AnomalyId: latestAnomaly?.Id,
                IncidentId: incident.Id,
                RecoveryActionId: recoveryAction.Id,
                AiAnalysis: analysis,
                RunbookId: runbook.Id,
                SsmCommandId: ssmResult.CommandId,
                ExecutionOutput: ssmResult.Success ? ssmResult.Output : (ssmResult.Error ?? ssmResult.Output),
                ExecutedViaSsm: true);
        }

        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        recoveryAction.Description += "\n\n[Simulated — SSM disabled or non-AWS service]";
        recoveryAction.ActionStatus = RecoveryActionStatus.Completed;
        await ResolveOpenIncidentsForServiceAsync(service.Id, cancellationToken);
        service.Status = ServiceStatus.Healthy;
        await db.SaveChangesAsync(cancellationToken);

        return new SelfHealingResult(
            Success: true,
            Message: $"Runbook '{runbook.Id}' completed (simulated): {service.Name}",
            AnomalyId: latestAnomaly?.Id,
            IncidentId: incident.Id,
            RecoveryActionId: recoveryAction.Id,
            AiAnalysis: analysis,
            RunbookId: runbook.Id,
            ExecutedViaSsm: false);
    }

    private static string GetRunbookEffect(string runbookId, string serviceName) => runbookId switch
    {
        "kill-stress" => $"Kills stress-test processes on {serviceName}. CPU and network load will drop immediately. Safe for production — only targets known load-test binaries.",
        "clear-temp-disk" => $"Removes temporary load-test files from /tmp on {serviceName}. Frees disk space without affecting application data.",
        "collect-diagnostics" => $"Gathers a snapshot of CPU, memory, disk, and top processes from {serviceName}. Read-only — makes no changes to the system.",
        _ => "Executes remediation commands on the target instance.",
    };

    private string? ResolveInstanceId(CloudService service)
    {
        if (Ec2InstanceIdPattern().IsMatch(service.Name))
            return service.Name;

        return configuration["AWS:Ec2InstanceId"]
            ?? Environment.GetEnvironmentVariable("AWS_EC2_INSTANCE_ID");
    }

    private static SelfHealingResult Failed(string message) =>
        new(false, message, null, null, null, null, null, null, null, false);
}
