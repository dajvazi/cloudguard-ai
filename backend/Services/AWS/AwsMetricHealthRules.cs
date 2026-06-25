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
