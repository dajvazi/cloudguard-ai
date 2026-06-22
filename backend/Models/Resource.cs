namespace CloudGuard.Api.Models;

public class Resource
{
    public int Id { get; set; }
    public string ResourceName { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string Status { get; set; } = Constants.ResourceStatus.Discovered;
    public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;
}
