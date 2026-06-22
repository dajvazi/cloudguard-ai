using CloudGuard.Api.DTOs;
using CloudGuard.Api.DTOs.Terraform;
using CloudGuard.Api.Models;

namespace CloudGuard.Api.Mappings;

public static class EntityMapper
{
    public static CloudServiceDto ToDto(this CloudService entity) =>
        new(
            entity.Id,
            entity.TerraformUploadId,
            entity.Name,
            entity.Type,
            entity.Status,
            entity.Description,
            entity.SourceKind,
            entity.RawResourceType,
            entity.SourceFile,
            entity.ModuleSource,
            entity.ParentModule,
            entity.CreatedAt);

    public static TerraformUploadDto ToDto(this TerraformUpload entity) =>
        new(
            entity.Id,
            entity.FileName,
            entity.UploadStatus,
            entity.ServicesDetected,
            entity.UploadedAt);

    public static TerraformUploadDetailDto ToDetailDto(this TerraformUpload entity) =>
        new(
            entity.Id,
            entity.FileName,
            entity.UploadStatus,
            entity.ServicesDetected,
            entity.UploadedAt,
            entity.CloudServices.Select(s => s.ToDto()).ToList());
}

public static class ResourceMapper
{
    public static ResourceDto ToDto(this Resource entity) =>
        new(
            entity.Id,
            entity.ResourceName,
            entity.ResourceType,
            entity.Source,
            entity.Status,
            entity.DiscoveredAt);
}
