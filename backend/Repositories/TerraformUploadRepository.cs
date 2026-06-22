using CloudGuard.Api.Data;
using CloudGuard.Api.Models;
using CloudGuard.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloudGuard.Api.Repositories;

public class TerraformUploadRepository(CloudGuardDbContext dbContext) : ITerraformUploadRepository
{
    public async Task<IReadOnlyList<TerraformUpload>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.TerraformUploads
            .AsNoTracking()
            .OrderByDescending(u => u.UploadedAt)
            .ToListAsync(cancellationToken);

    public async Task<TerraformUpload?> GetByIdWithServicesAsync(int id, CancellationToken cancellationToken = default) =>
        await dbContext.TerraformUploads
            .AsNoTracking()
            .Include(u => u.CloudServices)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task AddAsync(TerraformUpload upload, CancellationToken cancellationToken = default) =>
        await dbContext.TerraformUploads.AddAsync(upload, cancellationToken);

    public async Task LoadServicesAsync(TerraformUpload upload, CancellationToken cancellationToken = default) =>
        await dbContext.Entry(upload)
            .Collection(u => u.CloudServices)
            .LoadAsync(cancellationToken);
}
