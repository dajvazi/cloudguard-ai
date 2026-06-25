namespace CloudGuard.Api.Services.AWS;

public interface IAwsImportEvaluator
{
    Task<AwsEvaluationResult> EvaluateAsync(
        IReadOnlyList<AwsAlarmDto> alarms,
        CancellationToken ct = default);

    Task<AwsEvaluationResult> EvaluateExistingAsync(CancellationToken ct = default);
}

public record AwsEvaluationResult(int AnomaliesCreated, int IncidentsCreated);
