namespace CloudGuard.Api.Constants;

public static class IncidentStatus
{
    public const string Open = "Open";
    public const string Investigating = "Investigating";
    public const string Mitigating = "Mitigating";
    public const string Resolved = "Resolved";
}

public static class RecoveryActionStatus
{
    public const string Pending = "Pending";
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
}

public static class Severity
{
    public const string Info = "Info";
    public const string Warning = "Warning";
    public const string Critical = "Critical";
}
