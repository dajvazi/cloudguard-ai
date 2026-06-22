using CloudGuard.Api.Models;

namespace CloudGuard.Api.Repositories.Interfaces;

public interface ICloudServiceRepository
{
    Task<IReadOnlyList<CloudService>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CloudService?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CloudService>> GetByUploadIdAsync(int uploadId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
}
