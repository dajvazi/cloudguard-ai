namespace CloudGuard.Api.Services.AWS.Runbooks;

public record Runbook(
    string Id,
    string Name,
    string Description,
    IReadOnlyList<string> Commands);

public interface IRunbookService
{
    Runbook? Resolve(string anomalyType, string actionType, string serviceSourceKind);
    Runbook? GetById(string runbookId);
    IReadOnlyList<Runbook> GetAll();
}

public class RunbookService : IRunbookService
{
    private static readonly Dictionary<string, Runbook> Catalog = new(StringComparer.OrdinalIgnoreCase)
    {
        ["kill-stress"] = new Runbook(
            Id: "kill-stress",
            Name: "Kill CPU/Network Stress",
            Description: "Stops load-test processes (stress, curl loops) safely",
            Commands:
            [
                "pkill stress 2>/dev/null || true",
                "pkill -f 'while true' 2>/dev/null || true",
                "pkill -f 'speed.hetzner.de' 2>/dev/null || true",
                "rm -f /tmp/cloudguard_loadtest /tmp/testload 2>/dev/null || true",
                "echo 'CloudGuard runbook: kill-stress completed'",
                "uptime",
            ]),

        ["clear-temp-disk"] = new Runbook(
            Id: "clear-temp-disk",
            Name: "Clear Temp Disk Files",
            Description: "Removes temporary load-test files from /tmp",
            Commands:
            [
                "rm -f /tmp/cloudguard_loadtest /tmp/testload 2>/dev/null || true",
                "df -h /tmp",
                "echo 'CloudGuard runbook: clear-temp-disk completed'",
            ]),

        ["collect-diagnostics"] = new Runbook(
            Id: "collect-diagnostics",
            Name: "Collect Diagnostics",
            Description: "Gathers CPU, memory, and disk snapshot for incident review",
            Commands:
            [
                "echo '=== CloudGuard diagnostics ==='",
                "uptime",
                "free -m",
                "df -h",
                "ps aux --sort=-%cpu | head -5",
            ]),
    };

    public Runbook? GetById(string runbookId) =>
        Catalog.GetValueOrDefault(runbookId);

    public IReadOnlyList<Runbook> GetAll() =>
        Catalog.Values.ToList();

    public Runbook? Resolve(string anomalyType, string actionType, string serviceSourceKind)
    {
        if (!string.Equals(serviceSourceKind, "aws", StringComparison.OrdinalIgnoreCase))
            return null;

        var anomaly = anomalyType.ToLowerInvariant();
        var action = actionType.ToLowerInvariant();

        if (anomaly.Contains("cpu") || anomaly.Contains("cloudwatch") || action.Contains("scale") || action.Contains("restart"))
            return Catalog["kill-stress"];

        if (anomaly.Contains("memory") || action.Contains("cache"))
            return Catalog["kill-stress"];

        if (action.Contains("rollback") || action.Contains("credential"))
            return Catalog["collect-diagnostics"];

        return Catalog["kill-stress"];
    }
}
