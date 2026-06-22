namespace CloudGuard.Api.Models;

public class CloudService
{
    public int Id { get; set; }
    public int? TerraformUploadId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = Constants.ServiceStatus.Healthy;
    public string? Description { get; set; }
    public string SourceKind { get; set; } = string.Empty;
    public string? RawResourceType { get; set; }
    public string? SourceFile { get; set; }
    public string? ModuleSource { get; set; }
    public string? ParentModule { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public TerraformUpload? TerraformUpload { get; set; }
    public ICollection<Metric> Metrics { get; set; } = [];
    public ICollection<Anomaly> Anomalies { get; set; } = [];
    public ICollection<Incident> Incidents { get; set; } = [];
}
