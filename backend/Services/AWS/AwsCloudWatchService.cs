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
            // 1. Fetch alarms
            var alarmsResponse = await cloudWatch.DescribeAlarmsAsync(new DescribeAlarmsRequest(), ct);
            foreach (var alarm in alarmsResponse.MetricAlarms)
            {
                alarms.Add(new AwsAlarmDto(
                    AlarmName: alarm.AlarmName,
                    Namespace: alarm.Namespace,
                    MetricName: alarm.MetricName,
                    StateValue: alarm.StateValue.Value,
                    StateReason: alarm.StateReason,
                    Threshold: (decimal)alarm.Threshold,
                    ComparisonOperator: alarm.ComparisonOperator.Value,
                    StateUpdatedAt: alarm.StateUpdatedTimestamp));

                discoveredServices.Add($"{alarm.Namespace}/{alarm.MetricName}");
            }

            // 2. Fetch metric data
            var namespaces = string.IsNullOrEmpty(request.Namespace)
                ? new[] { "AWS/EC2", "AWS/RDS", "AWS/Lambda", "AWS/ECS", "AWS/ELB" }
                : new[] { request.Namespace };

            var endTime = DateTime.UtcNow;
            var startTime = endTime.AddMinutes(-request.PeriodMinutes);

            foreach (var ns in namespaces)
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
                            Period = 300,
                            Stat = "Average",
                        },
                    });

                    metricMap[id] = (metric.MetricName, instanceDim?.Value);
                    discoveredServices.Add(instanceDim?.Value ?? $"{ns}/{metric.MetricName}");
                }

                if (metricQueries.Count == 0) continue;

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

                    metrics.Add(new AwsMetricDataDto(
                        Namespace: ns,
                        MetricName: metricName ?? result.Label ?? "Unknown",
                        InstanceId: instanceId,
                        Average: values.Count > 0 ? Math.Round(values.Average(), 2) : 0,
                        Maximum: values.Count > 0 ? Math.Round(values.Max(), 2) : 0,
                        Minimum: values.Count > 0 ? Math.Round(values.Min(), 2) : 0,
                        Timestamp: timestamps.Count > 0 ? timestamps[0] : DateTime.UtcNow));
                }
            }

            // 3. Persist to DB: overwrite old AWS-imported data
            await PersistToDatabase(metrics, ct);

            logger.LogInformation(
                "AWS import complete: {Alarms} alarms, {Metrics} metrics, {Services} services",
                alarms.Count, metrics.Count, discoveredServices.Count);

            return new AwsImportResult(
                Success: true,
                Message: $"Imported {alarms.Count} alarms, {metrics.Count} metrics from {discoveredServices.Count} services",
                AlarmsImported: alarms.Count,
                MetricsImported: metrics.Count,
                ServicesDiscovered: discoveredServices.Count,
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
                Alarms: [],
                Metrics: []);
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

            dbContext.CloudServices.Add(service);
        }

        await dbContext.SaveChangesAsync(ct);
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
