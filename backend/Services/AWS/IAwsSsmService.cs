namespace CloudGuard.Api.Services.AWS;

public interface IAwsSsmService
{
    bool IsEnabled { get; }
    Task<SsmInstanceStatus> GetInstanceStatusAsync(string instanceId, CancellationToken ct = default);
    Task<SsmExecutionResult> ExecuteRunbookAsync(
        string instanceId,
        IReadOnlyList<string> commands,
        CancellationToken ct = default);
}

public record SsmInstanceStatus(
    string InstanceId,
    string PingStatus,
    bool Ready,
    string Message);

public record SsmExecutionResult(
    bool Success,
    string CommandId,
    string Status,
    string Output,
    string? Error);
