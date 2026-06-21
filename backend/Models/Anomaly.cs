namespace CloudGuard.Api.Models;

public class Anomaly
{
    public int Id { get; set; }
    public int CloudServiceId { get; set; }
    public string? AnomalyType { get; set; }
    public string? Severity { get; set; }
    public decimal? AiConfidence { get; set; }
    public string? Description { get; set; }
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    public CloudService CloudService { get; set; } = null!;
}
