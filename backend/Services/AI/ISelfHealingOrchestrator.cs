using CloudGuard.Api.DTOs;

namespace CloudGuard.Api.Services.AI;

public interface ISelfHealingOrchestrator
{
    Task<SelfHealingResult> TriggerAsync(int serviceId, CancellationToken cancellationToken = default);
    Task<SelfHealingResult> TriggerFromAnomalyAsync(int anomalyId, CancellationToken cancellationToken = default);
    Task<SelfHealingResult> TriggerFromIncidentAsync(int incidentId, CancellationToken cancellationToken = default);
    Task<HealingAnalysis> AnalyzeAsync(int serviceId, CancellationToken cancellationToken = default);
    Task<SelfHealingResult> ExecuteRunbookAsync(
        int serviceId,
        string runbookId,
        int? incidentId = null,
        CancellationToken cancellationToken = default);
}

public record SelfHealingResult(
    bool Success,
    string Message,
    int? AnomalyId,
    int? IncidentId,
    int? RecoveryActionId,
    AiAnalysisResult? AiAnalysis,
    string? RunbookId = null,
    string? SsmCommandId = null,
    string? ExecutionOutput = null,
    bool ExecutedViaSsm = false);

public record HealingOption(
    string RunbookId,
    string Name,
    string Description,
    string Effect,
    bool Recommended);

public record HealingAnalysis(
    bool Success,
    string ServiceName,
    string? AnomalyType,
    AiAnalysisResult? AiAnalysis,
    IReadOnlyList<HealingOption> Options);
