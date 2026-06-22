namespace CloudGuard.Api.DTOs.Requests;

public record CreateMetricRequest(
    int CloudServiceId,
    decimal? CpuUsage,
    decimal? MemoryUsage,
    decimal? LatencyMs,
    decimal? ErrorRate);

public record CreateAnomalyRequest(
    int CloudServiceId,
    string? AnomalyType,
    string? Severity,
    decimal? AiConfidence,
    string? Description);

public record CreateIncidentRequest(
    int CloudServiceId,
    string Title,
    string? Severity,
    string? RootCause);

public record CreateRecoveryActionRequest(
    int IncidentId,
    string? ActionType,
    string? Description);

public record UpdateIncidentStatusRequest(string Status);

public record UpdateRecoveryActionStatusRequest(string ActionStatus);
