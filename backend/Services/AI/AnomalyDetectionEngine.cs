using CloudGuard.Api.Constants;
using CloudGuard.Api.Data;
using CloudGuard.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudGuard.Api.Services.AI;

public class AnomalyDetectionEngine(
    IServiceScopeFactory scopeFactory,
    ILogger<AnomalyDetectionEngine> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);
    private static readonly int MinSamplesRequired = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Anomaly Detection Engine started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await AnalyzeMetricsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during anomaly detection cycle");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task AnalyzeMetricsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CloudGuardDbContext>();

        var services = await db.CloudServices
            .AsNoTracking()
            .ToListAsync(ct);

        foreach (var service in services)
        {
            var metrics = await db.Metrics
                .Where(m => m.CloudServiceId == service.Id)
                .OrderByDescending(m => m.RecordedAt)
                .Take(20)
                .ToListAsync(ct);

            if (metrics.Count < MinSamplesRequired) continue;

            var latest = metrics[0];
            var historical = metrics.Skip(1).ToList();

            var detectedAnomalies = DetectAnomalies(service, latest, historical);

            foreach (var anomaly in detectedAnomalies)
            {
                var alreadyExists = await db.Anomalies
                    .AnyAsync(a =>
                        a.CloudServiceId == service.Id &&
                        a.AnomalyType == anomaly.AnomalyType &&
                        a.DetectedAt > DateTime.UtcNow.AddMinutes(-10), ct);

                if (alreadyExists) continue;

                db.Anomalies.Add(anomaly);
                logger.LogWarning(
                    "Anomaly detected: {Type} on {Service} (confidence: {Confidence}%)",
                    anomaly.AnomalyType, service.Name, anomaly.AiConfidence);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static List<Anomaly> DetectAnomalies(
        CloudService service,
        Metric latest,
        List<Metric> historical)
    {
        var anomalies = new List<Anomaly>();

        CheckThreshold(anomalies, service, "High CPU Usage",
            latest.CpuUsage, historical.Select(m => m.CpuUsage).ToList(),
            absoluteThreshold: 85, deviationMultiplier: 2.5m);

        CheckThreshold(anomalies, service, "High Memory Usage",
            latest.MemoryUsage, historical.Select(m => m.MemoryUsage).ToList(),
            absoluteThreshold: 88, deviationMultiplier: 2.5m);

        CheckThreshold(anomalies, service, "Latency Spike",
            latest.LatencyMs, historical.Select(m => m.LatencyMs).ToList(),
            absoluteThreshold: 500, deviationMultiplier: 3m);

        CheckThreshold(anomalies, service, "Error Rate Surge",
            latest.ErrorRate, historical.Select(m => m.ErrorRate).ToList(),
            absoluteThreshold: 5, deviationMultiplier: 2m);

        return anomalies;
    }

    private static void CheckThreshold(
        List<Anomaly> anomalies,
        CloudService service,
        string anomalyType,
        decimal? currentValue,
        List<decimal?> historicalValues,
        decimal absoluteThreshold,
        decimal deviationMultiplier)
    {
        if (currentValue is null) return;

        var validValues = historicalValues.Where(v => v.HasValue).Select(v => v!.Value).ToList();
        if (validValues.Count < 2) return;

        var mean = validValues.Average();
        var stdDev = CalculateStdDev(validValues, mean);

        var exceedsAbsolute = currentValue.Value >= absoluteThreshold;
        var exceedsStatistical = stdDev > 0 && currentValue.Value > mean + (deviationMultiplier * stdDev);

        if (!exceedsAbsolute && !exceedsStatistical) return;

        var deviation = stdDev > 0 ? (currentValue.Value - mean) / stdDev : 0;
        var confidence = Math.Min(99m, 60m + (deviation * 8m));
        if (exceedsAbsolute && !exceedsStatistical) confidence = Math.Max(75m, confidence);

        var severity = confidence switch
        {
            >= 90 => Severity.Critical,
            >= 75 => Severity.Warning,
            _ => Severity.Info,
        };

        anomalies.Add(new Anomaly
        {
            CloudServiceId = service.Id,
            AnomalyType = anomalyType,
            Severity = severity,
            AiConfidence = Math.Round(confidence, 1),
            Description = $"{anomalyType}: current {currentValue.Value:F1} vs avg {mean:F1} ({deviation:F1}σ deviation)",
            DetectedAt = DateTime.UtcNow,
        });
    }

    private static decimal CalculateStdDev(List<decimal> values, decimal mean)
    {
        if (values.Count < 2) return 0;
        var sumSquares = values.Sum(v => (v - mean) * (v - mean));
        return (decimal)Math.Sqrt((double)(sumSquares / (values.Count - 1)));
    }
}
