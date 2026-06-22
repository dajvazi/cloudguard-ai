using CloudGuard.Api.DTOs;
using CloudGuard.Api.DTOs.Requests;

namespace CloudGuard.Api.Services.Metrics;

public interface IMetricService
{
    Task<IReadOnlyList<MetricDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<MetricDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MetricDto>> GetByServiceIdAsync(int serviceId, CancellationToken cancellationToken = default);
    Task<MetricDto> CreateAsync(CreateMetricRequest request, CancellationToken cancellationToken = default);
}
