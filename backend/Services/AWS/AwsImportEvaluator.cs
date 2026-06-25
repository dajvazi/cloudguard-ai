using CloudGuard.Api.Constants;
using CloudGuard.Api.Data;
using CloudGuard.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudGuard.Api.Services.AWS;

public class AwsImportEvaluator(
    CloudGuardDbContext db,
    ILogger<AwsImportEvaluator> logger) : IAwsImportEvaluator
{
    private static readonly TimeSpan DuplicateWindow = TimeSpan.FromMinutes(15);

    public Task<AwsEvaluationResult> EvaluateAsync(
        IReadOnlyList<AwsAlarmDto> alarms,
        CancellationToken ct = default) =>
        EvaluateInternalAsync(alarms, ct);

    public Task<AwsEvaluationResult> EvaluateExistingAsync(CancellationToken ct = default) =>
        EvaluateInternalAsync([], ct);

    private async Task<AwsEvaluationResult> EvaluateInternalAsync(
        IReadOnlyList<AwsAlarmDto> alarms,
        CancellationToken ct)
    {
        var anomaliesCreated = 0;
        var incidentsCreated = 0;
        var changed = false;

        var awsServices = await db.CloudServices
            .Include(s => s.Metrics)
            .Where(s => s.SourceKind == "aws")
            .ToListAsync(ct);

        foreach (var alarm in alarms.Where(a =>
                     string.Equals(a.StateValue, "ALARM", StringComparison.OrdinalIgnoreCase)))
        {
            var service = ResolveServiceForAlarm(awsServices, alarm);
            if (service is null) continue;

            var anomalyType = $"CloudWatch Alarm: {alarm.MetricName}";
            var description =
                $"{alarm.AlarmName} is in ALARM state. {alarm.StateReason ?? "Threshold breached."}";

            var (a, i, c) = await EnsureAnomalyAndIncidentAsync(
                service, anomalyType, Severity.Critical, 92m, description, ct);
            anomaliesCreated += a;
            incidentsCreated += i;
            changed |= c;
        }

        foreach (var service in awsServices)
        {
            foreach (var rule in AwsMetricHealthRules.All)
            {
                var peak = AwsMetricHealthRules.PeakForRule(service, rule);
                if (peak is null || peak < rule.Threshold) continue;

                var description =
                    $"{rule.AnomalyType}: {rule.MetricName} peak {peak:F1} (threshold {rule.Threshold:F0})";

                var (a, i, c) = await EnsureAnomalyAndIncidentAsync(
                    service,
                    rule.AnomalyType,
                    rule.Severity,
                    rule.Confidence,
                    description,
                    ct);
                anomaliesCreated += a;
                incidentsCreated += i;
                changed |= c;
            }
        }

        if (changed)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation(
                "AWS evaluation: {Anomalies} new anomalies, {Incidents} new incidents",
                anomaliesCreated, incidentsCreated);
        }

        return new AwsEvaluationResult(anomaliesCreated, incidentsCreated);
    }

    private static CloudService? ResolveServiceForAlarm(
        IReadOnlyList<CloudService> services,
        AwsAlarmDto alarm)
    {
        if (!string.IsNullOrWhiteSpace(alarm.InstanceId))
        {
            var byInstance = services.FirstOrDefault(s =>
                string.Equals(s.Name, alarm.InstanceId, StringComparison.OrdinalIgnoreCase));
            if (byInstance is not null) return byInstance;
        }

        var ns = alarm.Namespace.Replace("AWS/", "", StringComparison.OrdinalIgnoreCase);
        return services.FirstOrDefault(s =>
            string.Equals(s.Type, ns, StringComparison.OrdinalIgnoreCase))
            ?? services.FirstOrDefault();
    }

  private async Task<(int Anomalies, int Incidents, bool Changed)> EnsureAnomalyAndIncidentAsync(
        CloudService service,
        string anomalyType,
        string severity,
        decimal confidence,
        string description,
        CancellationToken ct)
    {
        var created = false;
        var anomalies = 0;
        var incidents = 0;

        AwsMetricHealthRules.ApplyServiceStatus(service, severity);
        created = true;

        var recentCutoff = DateTime.UtcNow.Subtract(DuplicateWindow);
        var duplicateAnomaly = await db.Anomalies.AnyAsync(a =>
            a.CloudServiceId == service.Id &&
            a.AnomalyType == anomalyType &&
            a.DetectedAt > recentCutoff, ct);

        if (!duplicateAnomaly)
        {
            db.Anomalies.Add(new Anomaly
            {
                CloudServiceId = service.Id,
                AnomalyType = anomalyType,
                Severity = severity,
                AiConfidence = confidence,
                Description = description,
                DetectedAt = DateTime.UtcNow,
            });
            anomalies = 1;
            created = true;
        }

        var openIncidentExists = await db.Incidents.AnyAsync(i =>
            i.CloudServiceId == service.Id &&
            i.Status != IncidentStatus.Resolved &&
            i.Title.Contains(anomalyType), ct);

        if (!openIncidentExists)
        {
            db.Incidents.Add(new Incident
            {
                CloudServiceId = service.Id,
                Title = $"[AWS] {anomalyType} on {service.Name}",
                Severity = severity,
                Status = IncidentStatus.Open,
                RootCause = description,
                CreatedAt = DateTime.UtcNow,
            });
            incidents = 1;
            created = true;
        }

        return (anomalies, incidents, created);
    }
}
