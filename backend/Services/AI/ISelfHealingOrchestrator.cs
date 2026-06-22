using CloudGuard.Api.DTOs;

namespace CloudGuard.Api.Services.AI;

public interface ISelfHealingOrchestrator
{
    Task<SelfHealingResult> TriggerAsync(int serviceId, CancellationToken cancellationToken = default);
    Task<SelfHealingResult> TriggerFromAnomalyAsync(int anomalyId, CancellationToken cancellationToken = default);
}

public record SelfHealingResult(
    bool Success,
    string Message,
    int? AnomalyId,
    int? IncidentId,
    int? RecoveryActionId,
    AiAnalysisResult? AiAnalysis);
