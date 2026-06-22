using CloudGuard.Api.DTOs;
using CloudGuard.Api.DTOs.Requests;

namespace CloudGuard.Api.Services.Resources;

public interface IResourceService
{
    Task<IReadOnlyList<ResourceDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ResourceDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ResourceDto>> GetBySourceAsync(string source, CancellationToken cancellationToken = default);
    Task<ResourceDto> CreateAsync(CreateResourceRequest request, CancellationToken cancellationToken = default);
}
