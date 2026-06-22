using CloudGuard.Api.Models;

namespace CloudGuard.Api.Repositories.Interfaces;

public interface ITerraformUploadRepository
{
    Task<IReadOnlyList<TerraformUpload>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TerraformUpload?> GetByIdWithServicesAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(TerraformUpload upload, CancellationToken cancellationToken = default);
    Task LoadServicesAsync(TerraformUpload upload, CancellationToken cancellationToken = default);
}
