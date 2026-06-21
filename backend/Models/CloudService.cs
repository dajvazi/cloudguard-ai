namespace CloudGuard.Api.Models;

public class CloudService
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = "Healthy";
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Metric> Metrics { get; set; } = [];
    public ICollection<Anomaly> Anomalies { get; set; } = [];
    public ICollection<Incident> Incidents { get; set; } = [];
}
