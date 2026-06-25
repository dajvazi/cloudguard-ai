namespace CloudGuard.Api.Models;

public class Metric
{
    public int Id { get; set; }
    public int CloudServiceId { get; set; }
    public string? MetricName { get; set; }
    public string? Unit { get; set; }
    public decimal? CpuUsage { get; set; }
    public decimal? MemoryUsage { get; set; }
    public decimal? NetworkIn { get; set; }
    public decimal? NetworkOut { get; set; }
    public decimal? DiskReadBytes { get; set; }
    public decimal? DiskWriteBytes { get; set; }
    public decimal? LatencyMs { get; set; }
    public decimal? ErrorRate { get; set; }
    public decimal? Value { get; set; }
    public decimal? Maximum { get; set; }
    public decimal? Minimum { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public CloudService CloudService { get; set; } = null!;
}
