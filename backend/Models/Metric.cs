namespace CloudGuard.Api.Models;

public class Metric
{
    public int Id { get; set; }
    public int CloudServiceId { get; set; }
    public decimal? CpuUsage { get; set; }
    public decimal? MemoryUsage { get; set; }
    public decimal? LatencyMs { get; set; }
    public decimal? ErrorRate { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public CloudService CloudService { get; set; } = null!;
}
