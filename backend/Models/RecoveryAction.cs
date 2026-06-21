namespace CloudGuard.Api.Models;

public class RecoveryAction
{
    public int Id { get; set; }
    public int IncidentId { get; set; }
    public string? ActionType { get; set; }
    public string ActionStatus { get; set; } = "Pending";
    public string? Description { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    public Incident Incident { get; set; } = null!;
}
