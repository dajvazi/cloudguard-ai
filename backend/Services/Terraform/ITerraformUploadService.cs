using CloudGuard.Api.DTOs.Terraform;

namespace CloudGuard.Api.Services.Terraform;

public interface ITerraformUploadService
{
    Task<TerraformUploadDetailDto> UploadAsync(IFormFile file, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TerraformUploadDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TerraformUploadDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
