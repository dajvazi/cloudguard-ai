using CloudGuard.Api.Data;
using CloudGuard.Api.Models;
using CloudGuard.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloudGuard.Api.Repositories;

public class ResourceRepository(CloudGuardDbContext dbContext) : IResourceRepository
{
    public async Task<IReadOnlyList<Resource>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Resources
            .AsNoTracking()
            .OrderByDescending(r => r.DiscoveredAt)
            .ToListAsync(cancellationToken);

    public async Task<Resource?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await dbContext.Resources
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Resource>> GetBySourceAsync(
        string source,
        CancellationToken cancellationToken = default) =>
        await dbContext.Resources
            .AsNoTracking()
            .Where(r => r.Source != null && r.Source.Contains(source))
            .OrderByDescending(r => r.DiscoveredAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Resource resource, CancellationToken cancellationToken = default) =>
        await dbContext.Resources.AddAsync(resource, cancellationToken);

    public async Task AddRangeAsync(IEnumerable<Resource> resources, CancellationToken cancellationToken = default) =>
        await dbContext.Resources.AddRangeAsync(resources, cancellationToken);
}
