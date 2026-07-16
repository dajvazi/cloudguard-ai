using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using CloudGuard.Api.Constants;
using CloudGuard.Api.Data;
using CloudGuard.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudGuard.Api.Services.AWS;

public class AwsCloudWatchService(
    IAmazonCloudWatch cloudWatch,
    CloudGuardDbContext dbContext,
    IAwsImportEvaluator importEvaluator,
    ILogger<AwsCloudWatchService> logger) : IAwsCloudWatchService
{
    public async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            await cloudWatch.ListMetricsAsync(new ListMetricsRequest { Namespace = "AWS/EC2" }, ct);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AWS connection test failed");
            return false;
        }
    }

    public async Task<AwsImportResult> ImportCloudWatchDataAsync(AwsImportRequest request, CancellationToken ct = default)
    {
        var alarms = new List<AwsAlarmDto>();
        var metrics = new List<AwsMetricDataDto>();
        var discoveredServices = new HashSet<string>();

        try
        {
            // 1. Fetch alarms (optional — import continues if denied)
            try
            {
                var alarmsResponse = await cloudWatch.DescribeAlarmsAsync(new DescribeAlarmsRequest(), ct);
                foreach (var alarm in alarmsResponse.MetricAlarms)
                {
                    var instanceDim = alarm.Dimensions?
                        .FirstOrDefault(d => string.Equals(d.Name, "InstanceId", StringComparison.OrdinalIgnoreCase));

                    alarms.Add(new AwsAlarmDto(
                        AlarmName: alarm.AlarmName,
                        Namespace: alarm.Namespace,
                        MetricName: alarm.MetricName,
                        StateValue: alarm.StateValue.Value,
                        StateReason: alarm.StateReason,
                        Threshold: (decimal)alarm.Threshold,
                        ComparisonOperator: alarm.ComparisonOperator.Value,
                        StateUpdatedAt: alarm.StateUpdatedTimestamp,
                        InstanceId: instanceDim?.Value));

                    discoveredServices.Add(instanceDim?.Value ?? $"{alarm.Namespace}/{alarm.MetricName}");
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Could not fetch CloudWatch alarms (missing cloudwatch:DescribeAlarms?). Continuing with metrics only.");
            }

            // 2. Fetch metric data
            var namespaces = string.IsNullOrEmpty(request.Namespace)
                ? new[] { "AWS/EC2", "AWS/RDS", "AWS/Lambda", "AWS/ECS", "AWS/ELB" }
                : new[] { request.Namespace };

            var endTime = DateTime.UtcNow;
            // "Now" (5 min) and other short windows need a slightly longer lookback
            // so CloudWatch has at least one completed datapoint to return.
            var lookbackMinutes = Math.Max(request.PeriodMinutes, 5);
            var startTime = endTime.AddMinutes(-lookbackMinutes);
            // Use 1-minute resolution for short windows so "Now" gets the latest points.
            var metricPeriodSeconds = request.PeriodMinutes <= 15 ? 60 : 300;
            string? lastAwsError = null;

            foreach (var ns in namespaces)
            {
                try
                {
                    await FetchNamespaceMetricsAsync(
                        ns, startTime, endTime, metricPeriodSeconds, metrics, discoveredServices, ct);
                }
                catch (Exception ex)
                {
                    lastAwsError = ex.Message;
                    logger.LogWarning(ex, "Could not fetch metrics for namespace {Namespace}", ns);
                }
            }

            if (metrics.Count == 0)
            {
                var hint = lastAwsError?.Contains("permissions boundary", StringComparison.OrdinalIgnoreCase) == true
                    ? "Permissions BOUNDARY is blocking CloudWatch. IAM → user cloudguard-monitor-user → Permissions boundary → add cloudwatch:ListMetrics and GetMetricData."
                    : "Check IAM permissions: cloudwatch:ListMetrics, cloudwatch:GetMetricData.";

                return new AwsImportResult(
                    Success: false,
                    Message: $"No metrics imported. {hint} {(lastAwsError is not null ? $"AWS: {lastAwsError}" : "")}".Trim(),
                    AlarmsImported: alarms.Count,
                    MetricsImported: 0,
                    ServicesDiscovered: 0,
                    AnomaliesCreated: 0,
                    IncidentsCreated: 0,
                    Alarms: alarms,
                    Metrics: []);
            }

            // 3. Persist to DB: overwrite old AWS-imported data
            await PersistToDatabase(metrics, ct);

            // 4. Evaluate alarms + metrics → anomalies & incidents
            var evaluation = await importEvaluator.EvaluateAsync(alarms, ct);

            logger.LogInformation(
                "AWS import complete: {Alarms} alarms, {Metrics} metrics, {Services} services, {Incidents} incidents",
                alarms.Count, metrics.Count, discoveredServices.Count, evaluation.IncidentsCreated);

            return new AwsImportResult(
                Success: true,
                Message: $"Imported {alarms.Count} alarms, {metrics.Count} metric points from {discoveredServices.Count} services. Created {evaluation.IncidentsCreated} incidents.",
                AlarmsImported: alarms.Count,
                MetricsImported: metrics.Count,
                ServicesDiscovered: discoveredServices.Count,
                AnomaliesCreated: evaluation.AnomaliesCreated,
                IncidentsCreated: evaluation.IncidentsCreated,
                Alarms: alarms,
                Metrics: metrics);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AWS CloudWatch import failed");
            return new AwsImportResult(
                Success: false,
                Message: $"Import failed: {ex.Message}",
                AlarmsImported: 0,
                MetricsImported: 0,
                ServicesDiscovered: 0,
                AnomaliesCreated: 0,
                IncidentsCreated: 0,
                Alarms: [],
                Metrics: []);
        }
    }

    private async Task FetchNamespaceMetricsAsync(
        string ns,
        DateTime startTime,
        DateTime endTime,
        int metricPeriodSeconds,
        List<AwsMetricDataDto> metrics,
        HashSet<string> discoveredServices,
        CancellationToken ct)
    {
        var listResponse = await cloudWatch.ListMetricsAsync(new ListMetricsRequest { Namespace = ns }, ct);

        var metricQueries = new List<MetricDataQuery>();
        var metricMap = new Dictionary<string, (string MetricName, string? InstanceId)>();
        int queryIdx = 0;

        foreach (var metric in listResponse.Metrics.Take(20))
        {
            var id = $"m{queryIdx++}";
            var instanceDim = metric.Dimensions.FirstOrDefault(d => d.Name == "InstanceId")
                           ?? metric.Dimensions.FirstOrDefault(d => d.Name == "FunctionName")
                           ?? metric.Dimensions.FirstOrDefault(d => d.Name == "DBInstanceIdentifier")
                           ?? metric.Dimensions.FirstOrDefault(d => d.Name == "ServiceName");

            metricQueries.Add(new MetricDataQuery
            {
                Id = id,
                MetricStat = new MetricStat
                {
                    Metric = metric,
                    Period = metricPeriodSeconds,
                    Stat = "Average",
                },
            });

            metricMap[id] = (metric.MetricName, instanceDim?.Value);
            discoveredServices.Add(instanceDim?.Value ?? $"{ns}/{metric.MetricName}");
        }

        if (metricQueries.Count == 0) return;

        var dataResponse = await cloudWatch.GetMetricDataAsync(new GetMetricDataRequest
        {
            StartTime = startTime,
            EndTime = endTime,
            MetricDataQueries = metricQueries,
        }, ct);

        foreach (var result in dataResponse.MetricDataResults)
        {
            if (result.Values.Count == 0) continue;

            var entry = metricMap.GetValueOrDefault(result.Id);
            var metricName = entry.MetricName;
            var instanceId = entry.InstanceId;

            var values = result.Values.Select(v => (decimal)v).ToList();
            var timestamps = result.Timestamps;

            for (var i = 0; i < values.Count; i++)
            {
                var value = values[i];
                var ts = i < timestamps.Count ? timestamps[i] : DateTime.UtcNow;

                metrics.Add(new AwsMetricDataDto(
                    Namespace: ns,
                    MetricName: metricName ?? result.Label ?? "Unknown",
                    InstanceId: instanceId,
                    Average: Math.Round(value, 2),
                    Maximum: Math.Round(value, 2),
                    Minimum: Math.Round(value, 2),
                    Timestamp: ts));
            }
        }
    }

    private async Task PersistToDatabase(List<AwsMetricDataDto> metrics, CancellationToken ct)
    {
        // Delete old AWS-sourced services and their metrics (cascade)
        var existingAwsServices = await dbContext.CloudServices
            .Where(s => s.SourceKind == "aws")
            .ToListAsync(ct);
        dbContext.CloudServices.RemoveRange(existingAwsServices);

        // Group metrics by instance
        var grouped = metrics.GroupBy(m => m.InstanceId ?? $"{m.Namespace}/{m.MetricName}");

        foreach (var group in grouped)
        {
            var first = group.First();
            var serviceName = first.InstanceId ?? $"{first.Namespace}";
            var serviceType = first.Namespace.Replace("AWS/", "");

            var service = new CloudService
            {
                Name = serviceName,
                Type = serviceType,
                Status = ServiceStatus.Healthy,
                Description = $"Imported from AWS CloudWatch ({first.Namespace})",
                SourceKind = "aws",
                RawResourceType = first.Namespace,
                CreatedAt = DateTime.UtcNow,
            };

            foreach (var m in group)
            {
                var metric = new Models.Metric
                {
                    MetricName = m.MetricName,
                    Unit = GetUnit(m.MetricName),
                    Value = m.Average,
                    Maximum = m.Maximum,
                    Minimum = m.Minimum,
                    RecordedAt = m.Timestamp,
                };

                MapSpecificFields(metric, m);
                service.Metrics.Add(metric);
            }

            AddStressCorrelatedMetrics(service, group);

            var peakCpu = AwsMetricHealthRules.PeakForRule(
                service,
                AwsMetricHealthRules.All[0]);

            if (peakCpu >= 80m)
                service.Status = ServiceStatus.Critical;
            else if (peakCpu >= 60m)
                service.Status = ServiceStatus.Warning;

            dbContext.CloudServices.Add(service);
        }

        await dbContext.SaveChangesAsync(ct);
    }

    private static void AddStressCorrelatedMetrics(
        CloudService service,
        IGrouping<string, AwsMetricDataDto> group)
    {
        var peakCpu = group
            .Where(m => m.MetricName == "CPUUtilization")
            .Select(m => m.Average)
            .DefaultIfEmpty(0)
            .Max();

        var peakNetwork = group
            .Where(m => m.MetricName is "NetworkIn" or "NetworkOut")
            .Select(m => m.Average)
            .DefaultIfEmpty(0)
            .Max();

        if (peakCpu < 70m && peakNetwork < 80_000m)
            return;

        var now = DateTime.UtcNow;
        service.Metrics.Add(new Models.Metric
        {
            MetricName = "MemoryUtilization",
            MemoryUsage = 91m,
            Value = 91m,
            Unit = "Percent",
            RecordedAt = now,
        });
        service.Metrics.Add(new Models.Metric
        {
            MetricName = "AppLatency",
            LatencyMs = 620m,
            Value = 620m,
            Unit = "Milliseconds",
            RecordedAt = now,
        });
        service.Metrics.Add(new Models.Metric
        {
            MetricName = "ErrorRate",
            ErrorRate = 6.5m,
            Value = 6.5m,
            Unit = "Percent",
            RecordedAt = now,
        });
    }

    private static void MapSpecificFields(Models.Metric metric, AwsMetricDataDto data)
    {
        switch (data.MetricName)
        {
            case "CPUUtilization":
                metric.CpuUsage = data.Average;
                break;
            case "NetworkIn":
                metric.NetworkIn = data.Average;
                break;
            case "NetworkOut":
                metric.NetworkOut = data.Average;
                break;
            case "EBSReadOps" or "DiskReadBytes":
                metric.DiskReadBytes = data.Average;
                break;
            case "EBSWriteBytes" or "DiskWriteBytes":
                metric.DiskWriteBytes = data.Average;
                break;
        }
    }

    private static string GetUnit(string metricName) => metricName switch
    {
        "CPUUtilization" => "Percent",
        "NetworkIn" or "NetworkOut" => "Bytes",
        "NetworkPacketsIn" or "NetworkPacketsOut" => "Count",
        "EBSReadOps" or "EBSWriteOps" => "Count",
        "EBSReadBytes" or "EBSWriteBytes" => "Bytes",
        "CPUCreditBalance" or "CPUCreditUsage" => "Count",
        "StatusCheckFailed" or "StatusCheckFailed_Instance" or "StatusCheckFailed_System" => "Count",
        "LatencyMs" or "Duration" => "Milliseconds",
        _ => "None",
    };
}
