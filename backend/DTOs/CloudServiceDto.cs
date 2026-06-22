namespace CloudGuard.Api.DTOs;

public record CloudServiceDto(
    int Id,
    int? TerraformUploadId,
    string Name,
    string Type,
    string Status,
    string? Description,
    string SourceKind,
    string? RawResourceType,
    string? SourceFile,
    string? ModuleSource,
    string? ParentModule,
    DateTime CreatedAt);
