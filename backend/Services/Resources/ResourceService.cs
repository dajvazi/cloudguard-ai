using CloudGuard.Api.Constants;
using CloudGuard.Api.DTOs;
using CloudGuard.Api.DTOs.Requests;
using CloudGuard.Api.Mappings;
using CloudGuard.Api.Models;
using CloudGuard.Api.Repositories.Interfaces;

namespace CloudGuard.Api.Services.Resources;

public class ResourceService(
    IResourceRepository resourceRepository,
    IUnitOfWork unitOfWork) : IResourceService
{
    public async Task<IReadOnlyList<ResourceDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var resources = await resourceRepository.GetAllAsync(cancellationToken);
        return resources.Select(r => r.ToDto()).ToList();
    }

    public async Task<ResourceDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var resource = await resourceRepository.GetByIdAsync(id, cancellationToken);
        return resource?.ToDto();
    }

    public async Task<IReadOnlyList<ResourceDto>> GetBySourceAsync(
        string source,
        CancellationToken cancellationToken = default)
    {
        var resources = await resourceRepository.GetBySourceAsync(source, cancellationToken);
        return resources.Select(r => r.ToDto()).ToList();
    }

    public async Task<ResourceDto> CreateAsync(
        CreateResourceRequest request,
        CancellationToken cancellationToken = default)
    {
        var resource = new Resource
        {
            ResourceName = request.ResourceName,
            ResourceType = request.ResourceType,
            Source = request.Source,
            Status = string.IsNullOrWhiteSpace(request.Status)
                ? ResourceStatus.Discovered
                : request.Status,
            DiscoveredAt = DateTime.UtcNow,
        };

        await resourceRepository.AddAsync(resource, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return resource.ToDto();
    }
}
