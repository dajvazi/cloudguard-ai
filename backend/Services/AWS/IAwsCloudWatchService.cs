namespace CloudGuard.Api.Services.AWS;

public interface IAwsCloudWatchService
{
    Task<AwsImportResult> ImportCloudWatchDataAsync(AwsImportRequest request, CancellationToken ct = default);
    Task<bool> TestConnectionAsync(CancellationToken ct = default);
}

public record AwsImportRequest(
    string Region,
    string? Namespace = null,
    int PeriodMinutes = 60);

public record AwsImportResult(
    bool Success,
    string Message,
    int AlarmsImported,
    int MetricsImported,
    int ServicesDiscovered,
    int AnomaliesCreated,
    int IncidentsCreated,
    List<AwsAlarmDto> Alarms,
    List<AwsMetricDataDto> Metrics);

public record AwsAlarmDto(
    string AlarmName,
    string Namespace,
    string MetricName,
    string StateValue,
    string? StateReason,
    decimal Threshold,
    string ComparisonOperator,
    DateTime? StateUpdatedAt,
    string? InstanceId = null);

public record AwsMetricDataDto(
    string Namespace,
    string MetricName,
    string? InstanceId,
    decimal Average,
    decimal Maximum,
    decimal Minimum,
    DateTime Timestamp);
