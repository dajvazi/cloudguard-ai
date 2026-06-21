namespace CloudGuard.Api.Models;

public class Incident
{
    public int Id { get; set; }
    public int CloudServiceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Severity { get; set; }
    public string Status { get; set; } = "Open";
    public string? RootCause { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }

    public CloudService CloudService { get; set; } = null!;
    public ICollection<RecoveryAction> RecoveryActions { get; set; } = [];
}
