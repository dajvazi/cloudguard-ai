namespace CloudGuard.Api.DTOs;

public record MetricDto(
    int Id,
    int CloudServiceId,
    string CloudServiceName,
    decimal? CpuUsage,
    decimal? MemoryUsage,
    decimal? LatencyMs,
    decimal? ErrorRate,
    DateTime RecordedAt);

public record AnomalyDto(
    int Id,
    int CloudServiceId,
    string CloudServiceName,
    string? AnomalyType,
    string? Severity,
    decimal? AiConfidence,
    string? Description,
    DateTime DetectedAt);

public record IncidentDto(
    int Id,
    int CloudServiceId,
    string CloudServiceName,
    string Title,
    string? Severity,
    string Status,
    string? RootCause,
    DateTime CreatedAt,
    DateTime? ResolvedAt);

public record IncidentDetailDto(
    int Id,
    int CloudServiceId,
    string CloudServiceName,
    string Title,
    string? Severity,
    string Status,
    string? RootCause,
    DateTime CreatedAt,
    DateTime? ResolvedAt,
    IReadOnlyList<RecoveryActionDto> RecoveryActions);

public record RecoveryActionDto(
    int Id,
    int IncidentId,
    string? ActionType,
    string ActionStatus,
    string? Description,
    DateTime ExecutedAt);
