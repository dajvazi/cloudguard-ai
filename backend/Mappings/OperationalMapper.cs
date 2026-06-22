using CloudGuard.Api.DTOs;
using CloudGuard.Api.Models;

namespace CloudGuard.Api.Mappings;

public static class OperationalMapper
{
    public static MetricDto ToDto(this Metric entity, string serviceName) =>
        new(
            entity.Id,
            entity.CloudServiceId,
            serviceName,
            entity.CpuUsage,
            entity.MemoryUsage,
            entity.LatencyMs,
            entity.ErrorRate,
            entity.RecordedAt);

    public static AnomalyDto ToDto(this Anomaly entity, string serviceName) =>
        new(
            entity.Id,
            entity.CloudServiceId,
            serviceName,
            entity.AnomalyType,
            entity.Severity,
            entity.AiConfidence,
            entity.Description,
            entity.DetectedAt);

    public static IncidentDto ToDto(this Incident entity, string serviceName) =>
        new(
            entity.Id,
            entity.CloudServiceId,
            serviceName,
            entity.Title,
            entity.Severity,
            entity.Status,
            entity.RootCause,
            entity.CreatedAt,
            entity.ResolvedAt);

    public static IncidentDetailDto ToDetailDto(this Incident entity, string serviceName) =>
        new(
            entity.Id,
            entity.CloudServiceId,
            serviceName,
            entity.Title,
            entity.Severity,
            entity.Status,
            entity.RootCause,
            entity.CreatedAt,
            entity.ResolvedAt,
            entity.RecoveryActions.Select(a => a.ToDto()).ToList());

    public static RecoveryActionDto ToDto(this RecoveryAction entity) =>
        new(
            entity.Id,
            entity.IncidentId,
            entity.ActionType,
            entity.ActionStatus,
            entity.Description,
            entity.ExecutedAt);
}
