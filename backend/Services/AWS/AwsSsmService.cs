using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;

namespace CloudGuard.Api.Services.AWS;

public class AwsSsmService(
    IAmazonSimpleSystemsManagement ssm,
    IConfiguration configuration,
    ILogger<AwsSsmService> logger) : IAwsSsmService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan MaxWait = TimeSpan.FromSeconds(90);

    public bool IsEnabled =>
        configuration.GetValue("AWS:SsmEnabled", true) &&
        !string.IsNullOrWhiteSpace(configuration["AWS:Ec2InstanceId"] ??
            Environment.GetEnvironmentVariable("AWS_EC2_INSTANCE_ID"));

    public async Task<SsmInstanceStatus> GetInstanceStatusAsync(
        string instanceId,
        CancellationToken ct = default)
    {
        try
        {
            var response = await ssm.DescribeInstanceInformationAsync(
                new DescribeInstanceInformationRequest
                {
                    Filters =
                    [
                        new InstanceInformationStringFilter
                        {
                            Key = "InstanceIds",
                            Values = [instanceId],
                        },
                    ],
                }, ct);

            var info = response.InstanceInformationList.FirstOrDefault();
            if (info is null)
            {
                return new SsmInstanceStatus(
                    instanceId,
                    "NotRegistered",
                    false,
                    "Instance not registered in SSM. Attach IAM role AmazonSSMManagedInstanceCore to EC2, " +
                    "start SSM Agent (sudo systemctl start amazon-ssm-agent), then check Fleet Manager.");
            }

            var ping = info.PingStatus?.Value ?? "Unknown";
            var online = string.Equals(ping, "Online", StringComparison.OrdinalIgnoreCase);

            return new SsmInstanceStatus(
                instanceId,
                ping,
                online,
                online
                    ? "SSM agent is online and ready for Run Command"
                    : $"SSM agent status is '{ping}'. Instance must be Online before self-healing can run.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to describe SSM instance {InstanceId}", instanceId);
            return new SsmInstanceStatus(
                instanceId,
                "Error",
                false,
                $"Could not check SSM status: {ex.Message}");
        }
    }

    public async Task<SsmExecutionResult> ExecuteRunbookAsync(
        string instanceId,
        IReadOnlyList<string> commands,
        CancellationToken ct = default)
    {
        var status = await GetInstanceStatusAsync(instanceId, ct);
        if (!status.Ready)
        {
            return new SsmExecutionResult(
                Success: false,
                CommandId: "",
                Status: "NotReady",
                Output: "",
                Error: status.Message);
        }

        try
        {
            logger.LogInformation("SSM SendCommand to {InstanceId}: {Commands}",
                instanceId, string.Join("; ", commands));

            var sendResponse = await ssm.SendCommandAsync(new SendCommandRequest
            {
                InstanceIds = [instanceId],
                DocumentName = "AWS-RunShellScript",
                Parameters = new Dictionary<string, List<string>>
                {
                    ["commands"] = commands.ToList(),
                },
            }, ct);

            var commandId = sendResponse.Command.CommandId;
            var deadline = DateTime.UtcNow.Add(MaxWait);

            while (DateTime.UtcNow < deadline)
            {
                await Task.Delay(PollInterval, ct);

                var invocation = await ssm.GetCommandInvocationAsync(new GetCommandInvocationRequest
                {
                    CommandId = commandId,
                    InstanceId = instanceId,
                }, ct);

                var invocationStatus = invocation.Status;

                if (invocationStatus == CommandInvocationStatus.Success)
                {
                    return new SsmExecutionResult(
                        Success: true,
                        CommandId: commandId,
                        Status: invocationStatus.ToString(),
                        Output: invocation.StandardOutputContent ?? "",
                        Error: null);
                }

                if (invocationStatus == CommandInvocationStatus.Failed
                    || invocationStatus == CommandInvocationStatus.Cancelled
                    || invocationStatus == CommandInvocationStatus.TimedOut)
                {
                    return new SsmExecutionResult(
                        Success: false,
                        CommandId: commandId,
                        Status: invocationStatus.ToString(),
                        Output: invocation.StandardOutputContent ?? "",
                        Error: invocation.StandardErrorContent ?? $"SSM command {invocationStatus}");
                }
            }

            return new SsmExecutionResult(
                Success: false,
                CommandId: commandId,
                Status: "TimedOut",
                Output: "",
                Error: "SSM command did not complete within timeout");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SSM execution failed for {InstanceId}", instanceId);
            return new SsmExecutionResult(
                Success: false,
                CommandId: "",
                Status: "Error",
                Output: "",
                Error: FormatSsmError(ex));
        }
    }

    private static string FormatSsmError(Exception ex)
    {
        var error = ex.Message;

        if (error.Contains("not authorized", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("AccessDenied", StringComparison.OrdinalIgnoreCase))
        {
            return
                "IAM permission denied. Add ssm:SendCommand, ssm:GetCommandInvocation, " +
                "ssm:DescribeInstanceInformation to cloudguard-monitor-user. " +
                $"Details: {ex.Message}";
        }

        if (error.Contains("valid state", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("not registered", StringComparison.OrdinalIgnoreCase))
        {
            return
                "EC2 instance is not managed by SSM (not Online). Fix: " +
                "1) Attach IAM role AmazonSSMManagedInstanceCore to the instance. " +
                "2) On EC2: sudo systemctl enable --now amazon-ssm-agent. " +
                "3) In AWS Console → Systems Manager → Fleet Manager, wait until Ping = Online. " +
                $"Details: {ex.Message}";
        }

        return ex.Message;
    }
}
