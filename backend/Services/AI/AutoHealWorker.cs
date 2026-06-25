using CloudGuard.Api.Constants;
using CloudGuard.Api.Data;
using CloudGuard.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudGuard.Api.Services.AI;

public class AutoHealWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<AutoHealWorker> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(45);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Auto-heal worker started (enabled: {Enabled})",
            configuration.GetValue("AWS:AutoHealEnabled", false));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (configuration.GetValue("AWS:AutoHealEnabled", false))
                    await ProcessOpenIncidentsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Auto-heal cycle failed");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task ProcessOpenIncidentsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CloudGuardDbContext>();
        var orchestrator = scope.ServiceProvider.GetRequiredService<ISelfHealingOrchestrator>();

        var openIncidents = await db.Incidents
            .Include(i => i.CloudService)
            .Where(i =>
                i.Status == IncidentStatus.Open &&
                i.CloudService.SourceKind == "aws")
            .OrderBy(i => i.CreatedAt)
            .Take(5)
            .ToListAsync(ct);

        foreach (var incident in openIncidents)
        {
            var hasRecovery = await db.RecoveryActions
                .AnyAsync(r => r.IncidentId == incident.Id, ct);

            if (hasRecovery) continue;

            logger.LogInformation(
                "Auto-heal triggering for incident {IncidentId} ({Title})",
                incident.Id, incident.Title);

            var result = await orchestrator.TriggerFromIncidentAsync(incident.Id, ct);

            if (result.Success)
            {
                logger.LogInformation(
                    "Auto-heal succeeded for incident {IncidentId} via {Runbook}",
                    incident.Id, result.RunbookId ?? "simulated");
            }
            else
            {
                logger.LogWarning(
                    "Auto-heal failed for incident {IncidentId}: {Message}",
                    incident.Id, result.Message);
            }
        }
    }
}
