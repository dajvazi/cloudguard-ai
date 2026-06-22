using CloudGuard.Api.DTOs;
using CloudGuard.Api.Mappings;
using CloudGuard.Api.Repositories.Interfaces;

namespace CloudGuard.Api.Services.CloudServices;

public class CloudServiceService(ICloudServiceRepository cloudServiceRepository) : ICloudServiceService
{
    public async Task<IReadOnlyList<CloudServiceDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var services = await cloudServiceRepository.GetAllAsync(cancellationToken);
        return services.Select(s => s.ToDto()).ToList();
    }

    public async Task<CloudServiceDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var service = await cloudServiceRepository.GetByIdAsync(id, cancellationToken);
        return service?.ToDto();
    }

    public async Task<IReadOnlyList<CloudServiceDto>> GetByUploadIdAsync(
        int uploadId,
        CancellationToken cancellationToken = default)
    {
        var services = await cloudServiceRepository.GetByUploadIdAsync(uploadId, cancellationToken);
        return services.Select(s => s.ToDto()).ToList();
    }
}
