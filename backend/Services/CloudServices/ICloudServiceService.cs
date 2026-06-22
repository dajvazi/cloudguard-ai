using CloudGuard.Api.DTOs;

namespace CloudGuard.Api.Services.CloudServices;

public interface ICloudServiceService
{
    Task<IReadOnlyList<CloudServiceDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CloudServiceDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CloudServiceDto>> GetByUploadIdAsync(
        int uploadId,
        CancellationToken cancellationToken = default);
}
