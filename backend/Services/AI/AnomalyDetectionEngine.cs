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
                .Take(100)
                .ToListAsync(ct);

            if (metrics.Count == 0) continue;

            var detectedAnomalies = new List<Anomaly>();

            var cpuMetrics = metrics
                .Where(m => m.MetricName == "CPUUtilization" && (m.CpuUsage.HasValue || m.Value.HasValue))
                .ToList();

            if (cpuMetrics.Count > 0)
            {
                var peakCpu = cpuMetrics.Max(m => m.CpuUsage ?? m.Value ?? 0);
                var latestCpu = cpuMetrics[0];
                var cpuHistory = cpuMetrics.Skip(1).ToList();

                CheckAbsoluteThreshold(
                    detectedAnomalies, service, "High CPU Usage", peakCpu, 80m, Severity.Critical);

                if (cpuHistory.Count >= 2)
                {
                    detectedAnomalies.AddRange(DetectAnomalies(
                        service, latestCpu, cpuHistory, useCpu: true));
                }
            }

            CheckPeakThreshold(detectedAnomalies, service, metrics, "NetworkIn",
                "High Network Traffic (In)", m => m.NetworkIn ?? m.Value, 100_000m, Severity.Warning);

            CheckPeakThreshold(detectedAnomalies, service, metrics, "NetworkOut",
                "High Network Traffic (Out)", m => m.NetworkOut ?? m.Value, 100_000m, Severity.Warning);

            CheckPeakThreshold(detectedAnomalies, service, metrics, "NetworkPacketsIn",
                "High Network Packet Rate", m => m.Value, 3_000m, Severity.Warning);

            CheckPeakThreshold(detectedAnomalies, service, metrics, "DiskReadBytes",
                "High Disk Read I/O", m => m.DiskReadBytes ?? m.Value, 50_000m, Severity.Warning);

            CheckPeakThreshold(detectedAnomalies, service, metrics, "DiskWriteBytes",
                "High Disk Write I/O", m => m.DiskWriteBytes ?? m.Value, 50_000m, Severity.Warning);

            CheckPeakThreshold(detectedAnomalies, service, metrics, "EBSReadBytes",
                "High Disk Read I/O", m => m.DiskReadBytes ?? m.Value, 50_000m, Severity.Warning);

            CheckPeakThreshold(detectedAnomalies, service, metrics, "EBSWriteBytes",
                "High Disk Write I/O", m => m.DiskWriteBytes ?? m.Value, 50_000m, Severity.Warning);

            CheckPeakThreshold(detectedAnomalies, service, metrics, "MemoryUtilization",
                "High Memory Usage", m => m.MemoryUsage ?? m.Value, 88m, Severity.Warning);

            CheckPeakThreshold(detectedAnomalies, service, metrics, "AppLatency",
                "Latency Spike", m => m.LatencyMs ?? m.Value, 500m, Severity.Critical);

            CheckPeakThreshold(detectedAnomalies, service, metrics, "ErrorRate",
                "Error Rate Surge", m => m.ErrorRate ?? m.Value, 5m, Severity.Critical);

            var latestMemory = LatestMetric(metrics, m => m.MemoryUsage.HasValue);
            var memoryHistory = HistoryFor(metrics, m => m.MemoryUsage.HasValue);

            var latestLatency = LatestMetric(metrics, m => m.LatencyMs.HasValue);
            var latencyHistory = HistoryFor(metrics, m => m.LatencyMs.HasValue);

            var latestError = LatestMetric(metrics, m => m.ErrorRate.HasValue);
            var errorHistory = HistoryFor(metrics, m => m.ErrorRate.HasValue);

            if (latestMemory is not null && memoryHistory.Count >= 2)
                detectedAnomalies.AddRange(DetectAnomalies(service, latestMemory, memoryHistory, useCpu: false));

            if (latestLatency is not null && latencyHistory.Count >= 2)
                detectedAnomalies.AddRange(DetectAnomalies(service, latestLatency, latencyHistory, useCpu: false, latencyMode: true));

            if (latestError is not null && errorHistory.Count >= 2)
                detectedAnomalies.AddRange(DetectAnomalies(service, latestError, errorHistory, useCpu: false, errorMode: true));

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

                if (anomaly.Severity is Severity.Critical or Severity.Warning)
                {
                    var openIncident = await db.Incidents.AnyAsync(i =>
                        i.CloudServiceId == service.Id &&
                        i.Status != IncidentStatus.Resolved &&
                        i.Title.Contains(anomaly.AnomalyType!), ct);

                    if (!openIncident)
                    {
                        db.Incidents.Add(new Incident
                        {
                            CloudServiceId = service.Id,
                            Title = $"[Auto] {anomaly.AnomalyType} on {service.Name}",
                            Severity = anomaly.Severity,
                            Status = IncidentStatus.Open,
                            RootCause = anomaly.Description,
                            CreatedAt = DateTime.UtcNow,
                        });

                        var trackedService = await db.CloudServices.FirstAsync(s => s.Id == service.Id, ct);
                        trackedService.Status = anomaly.Severity == Severity.Critical
                            ? ServiceStatus.Critical
                            : ServiceStatus.Warning;
                    }
                }
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static Metric? LatestMetric(
        List<Metric> metrics,
        Func<Metric, bool> predicate,
        Func<Metric, bool>? namePredicate = null) =>
        metrics.FirstOrDefault(m => predicate(m) && (namePredicate is null || namePredicate(m)));

    private static List<Metric> HistoryFor(
        List<Metric> metrics,
        Func<Metric, bool> predicate,
        Func<Metric, bool>? namePredicate = null) =>
        metrics.Where(m => predicate(m) && (namePredicate is null || namePredicate(m))).Skip(1).ToList();

    private static void CheckPeakThreshold(
        List<Anomaly> anomalies,
        CloudService service,
        List<Metric> metrics,
        string metricName,
        string anomalyType,
        Func<Metric, decimal?> valueSelector,
        decimal threshold,
        string severity)
    {
        var matching = metrics
            .Where(m => m.MetricName == metricName)
            .Select(valueSelector)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToList();

        if (matching.Count == 0) return;

        var peak = matching.Max();
        CheckAbsoluteThreshold(anomalies, service, anomalyType, peak, threshold, severity);
    }

    private static void CheckAbsoluteThreshold(
        List<Anomaly> anomalies,
        CloudService service,
        string anomalyType,
        decimal currentValue,
        decimal threshold,
        string severity)
    {
        if (currentValue < threshold) return;

        anomalies.Add(new Anomaly
        {
            CloudServiceId = service.Id,
            AnomalyType = anomalyType,
            Severity = severity,
            AiConfidence = Math.Min(99m, 70m + (currentValue - threshold)),
            Description = $"{anomalyType}: peak {currentValue:F1} (threshold {threshold:F0})",
            DetectedAt = DateTime.UtcNow,
        });
    }

    private static List<Anomaly> DetectAnomalies(
        CloudService service,
        Metric latest,
        List<Metric> historical,
        bool useCpu = false,
        bool latencyMode = false,
        bool errorMode = false)
    {
        var anomalies = new List<Anomaly>();

        if (useCpu)
        {
            CheckThreshold(anomalies, service, "High CPU Usage",
                latest.CpuUsage ?? latest.Value, historical.Select(m => m.CpuUsage ?? m.Value).ToList(),
                absoluteThreshold: 85, deviationMultiplier: 2.5m);
            return anomalies;
        }

        if (latencyMode)
        {
            CheckThreshold(anomalies, service, "Latency Spike",
                latest.LatencyMs, historical.Select(m => m.LatencyMs).ToList(),
                absoluteThreshold: 500, deviationMultiplier: 3m);
            return anomalies;
        }

        if (errorMode)
        {
            CheckThreshold(anomalies, service, "Error Rate Surge",
                latest.ErrorRate, historical.Select(m => m.ErrorRate).ToList(),
                absoluteThreshold: 5, deviationMultiplier: 2m);
            return anomalies;
        }

        CheckThreshold(anomalies, service, "High Memory Usage",
            latest.MemoryUsage, historical.Select(m => m.MemoryUsage).ToList(),
            absoluteThreshold: 88, deviationMultiplier: 2.5m);

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

        var exceedsAbsolute = currentValue.Value >= absoluteThreshold;

        var validValues = historicalValues.Where(v => v.HasValue).Select(v => v!.Value).ToList();
        if (validValues.Count < 2)
        {
            if (exceedsAbsolute)
            {
                anomalies.Add(new Anomaly
                {
                    CloudServiceId = service.Id,
                    AnomalyType = anomalyType,
                    Severity = currentValue.Value >= absoluteThreshold + 10 ? Severity.Critical : Severity.Warning,
                    AiConfidence = Math.Min(99m, 75m + (currentValue.Value - absoluteThreshold) / 2m),
                    Description = $"{anomalyType}: current {currentValue.Value:F1} exceeds threshold {absoluteThreshold:F0}",
                    DetectedAt = DateTime.UtcNow,
                });
            }
            return;
        }

        var mean = validValues.Average();
        var stdDev = CalculateStdDev(validValues, mean);

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
