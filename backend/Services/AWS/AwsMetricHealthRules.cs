using CloudGuard.Api.Constants;
using CloudGuard.Api.Models;

namespace CloudGuard.Api.Services.AWS;

internal static class AwsMetricHealthRules
{
    internal sealed record Rule(
        string MetricName,
        string AnomalyType,
        string Severity,
        decimal Threshold,
        decimal Confidence,
        Func<Metric, decimal?> ValueSelector);

    internal static readonly Rule[] All =
    [
        new(
            "CPUUtilization",
            "High CPU Usage",
            Severity.Critical,
            80m,
            92m,
            m => m.Maximum ?? m.CpuUsage ?? m.Value),
        new(
            "NetworkIn",
            "High Network Traffic (In)",
            Severity.Warning,
            100_000m,
            85m,
            m => m.Maximum ?? m.NetworkIn ?? m.Value),
        new(
            "NetworkOut",
            "High Network Traffic (Out)",
            Severity.Warning,
            100_000m,
            85m,
            m => m.Maximum ?? m.NetworkOut ?? m.Value),
        new(
            "NetworkPacketsIn",
            "High Network Packet Rate",
            Severity.Warning,
            3_000m,
            82m,
            m => m.Maximum ?? m.Value),
        new(
            "DiskReadBytes",
            "High Disk Read I/O",
            Severity.Warning,
            50_000m,
            80m,
            m => m.Maximum ?? m.DiskReadBytes ?? m.Value),
        new(
            "DiskWriteBytes",
            "High Disk Write I/O",
            Severity.Warning,
            50_000m,
            80m,
            m => m.Maximum ?? m.DiskWriteBytes ?? m.Value),
        new(
            "EBSReadBytes",
            "High Disk Read I/O",
            Severity.Warning,
            50_000m,
            80m,
            m => m.Maximum ?? m.DiskReadBytes ?? m.Value),
        new(
            "EBSWriteBytes",
            "High Disk Write I/O",
            Severity.Warning,
            50_000m,
            80m,
            m => m.Maximum ?? m.DiskWriteBytes ?? m.Value),
        new(
            "MemoryUtilization",
            "High Memory Usage",
            Severity.Warning,
            88m,
            88m,
            m => m.MemoryUsage ?? m.Value),
        new(
            "AppLatency",
            "Latency Spike",
            Severity.Critical,
            500m,
            90m,
            m => m.LatencyMs ?? m.Value),
        new(
            "ErrorRate",
            "Error Rate Surge",
            Severity.Critical,
            5m,
            93m,
            m => m.ErrorRate ?? m.Value),
        new(
            "StatusCheckFailed",
            "EC2 Status Check Failed",
            Severity.Critical,
            1m,
            95m,
            m => m.Maximum ?? m.Value),
        new(
            "StatusCheckFailed_Instance",
            "EC2 Instance Status Check Failed",
            Severity.Critical,
            1m,
            95m,
            m => m.Maximum ?? m.Value),
        new(
            "StatusCheckFailed_System",
            "EC2 System Status Check Failed",
            Severity.Warning,
            1m,
            85m,
            m => m.Maximum ?? m.Value),
    ];

    internal static decimal? PeakForRule(CloudService service, Rule rule)
    {
        var values = service.Metrics
            .Where(m => m.MetricName == rule.MetricName)
            .Select(rule.ValueSelector)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToList();

        return values.Count == 0 ? null : values.Max();
    }

    internal static void ApplyServiceStatus(CloudService service, string severity)
    {
        if (severity == Severity.Critical)
            service.Status = ServiceStatus.Critical;
        else if (service.Status == ServiceStatus.Healthy)
            service.Status = ServiceStatus.Warning;
    }
}
