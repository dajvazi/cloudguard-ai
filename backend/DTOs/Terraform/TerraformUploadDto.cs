namespace CloudGuard.Api.DTOs.Terraform;

public record TerraformUploadDto(
    int Id,
    string FileName,
    string UploadStatus,
    int ServicesDetected,
    DateTime UploadedAt);

public record TerraformUploadDetailDto(
    int Id,
    string FileName,
    string UploadStatus,
    int ServicesDetected,
    DateTime UploadedAt,
    IReadOnlyList<CloudServiceDto> Services);
