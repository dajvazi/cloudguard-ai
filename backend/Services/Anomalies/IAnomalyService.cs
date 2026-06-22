using CloudGuard.Api.DTOs;
using CloudGuard.Api.DTOs.Requests;

namespace CloudGuard.Api.Services.Anomalies;

public interface IAnomalyService
{
    Task<IReadOnlyList<AnomalyDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AnomalyDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AnomalyDto>> GetByServiceIdAsync(int serviceId, CancellationToken cancellationToken = default);
    Task<AnomalyDto> CreateAsync(CreateAnomalyRequest request, CancellationToken cancellationToken = default);
}
