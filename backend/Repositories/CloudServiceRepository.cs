using CloudGuard.Api.Data;
using CloudGuard.Api.Models;
using CloudGuard.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloudGuard.Api.Repositories;

public class CloudServiceRepository(CloudGuardDbContext dbContext) : ICloudServiceRepository
{
    public async Task<IReadOnlyList<CloudService>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.CloudServices
            .AsNoTracking()
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<CloudService?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await dbContext.CloudServices
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CloudService>> GetByUploadIdAsync(
        int uploadId,
        CancellationToken cancellationToken = default) =>
        await dbContext.CloudServices
            .AsNoTracking()
            .Where(s => s.TerraformUploadId == uploadId)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        await dbContext.CloudServices.AnyAsync(s => s.Id == id, cancellationToken);
}
