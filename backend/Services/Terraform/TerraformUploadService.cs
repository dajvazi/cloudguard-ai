using CloudGuard.Api.Constants;
using CloudGuard.Api.DTOs.Terraform;
using CloudGuard.Api.Mappings;
using CloudGuard.Api.Models;
using CloudGuard.Api.Repositories.Interfaces;

namespace CloudGuard.Api.Services.Terraform;

public class TerraformUploadService(
    ITerraformUploadRepository terraformUploadRepository,
    IResourceRepository resourceRepository,
    ITerraformArchiveExtractor archiveExtractor,
    ITerraformProjectParser projectParser,
    IUnitOfWork unitOfWork) : ITerraformUploadService
{
    public async Task<TerraformUploadDetailDto> UploadAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        var files = await archiveExtractor.ExtractAsync(file, cancellationToken);
        var parsedResources = projectParser.ParseProject(files);
        var discoveredAt = DateTime.UtcNow;

        var upload = new TerraformUpload
        {
            FileName = file.FileName,
            UploadStatus = UploadStatus.Processed,
            ServicesDetected = parsedResources.Count,
            UploadedAt = discoveredAt,
        };

        var infrastructureResources = new List<Resource>();

        foreach (var resource in parsedResources)
        {
            upload.CloudServices.Add(new CloudService
            {
                Name = resource.Name,
                Type = resource.DisplayType,
                Status = ServiceStatus.Healthy,
                Description = resource.Description,
                SourceKind = resource.SourceKind,
                RawResourceType = resource.ResourceType,
                SourceFile = resource.SourceFile,
                ModuleSource = resource.ModuleSource,
                ParentModule = resource.ParentModule,
            });

            infrastructureResources.Add(new Resource
            {
                ResourceName = resource.Name,
                ResourceType = resource.DisplayType,
                Source = resource.ModuleSource ?? resource.SourceFile ?? file.FileName,
                Status = ResourceStatus.Discovered,
                DiscoveredAt = discoveredAt,
            });
        }

        await terraformUploadRepository.AddAsync(upload, cancellationToken);
        await resourceRepository.AddRangeAsync(infrastructureResources, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await terraformUploadRepository.LoadServicesAsync(upload, cancellationToken);

        return upload.ToDetailDto();
    }

    public async Task<IReadOnlyList<TerraformUploadDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var uploads = await terraformUploadRepository.GetAllAsync(cancellationToken);
        return uploads.Select(u => u.ToDto()).ToList();
    }

    public async Task<TerraformUploadDetailDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var upload = await terraformUploadRepository.GetByIdWithServicesAsync(id, cancellationToken);
        return upload?.ToDetailDto();
    }
}
