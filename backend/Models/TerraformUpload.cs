namespace CloudGuard.Api.Models;

public class TerraformUpload
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string UploadStatus { get; set; } = "Uploaded";
    public int ServicesDetected { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
