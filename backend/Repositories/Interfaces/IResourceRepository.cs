using CloudGuard.Api.Models;

namespace CloudGuard.Api.Repositories.Interfaces;

public interface IResourceRepository
{
    Task<IReadOnlyList<Resource>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Resource?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Resource>> GetBySourceAsync(string source, CancellationToken cancellationToken = default);
    Task AddAsync(Resource resource, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<Resource> resources, CancellationToken cancellationToken = default);
}
