namespace CloudGuard.Api.Services.AI;

public interface IAiAnalysisService
{
    Task<AiAnalysisResult> AnalyzeIncidentAsync(
        string serviceName,
        string serviceType,
        string anomalyType,
        string anomalyDescription,
        CancellationToken cancellationToken = default);
}

public record AiAnalysisResult(
    string RootCause,
    string RecommendedAction,
    string ActionType,
    string Severity);
