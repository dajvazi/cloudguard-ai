using CloudGuard.Api.Models;
using CloudGuard.Api.Repositories.ReadModels;

namespace CloudGuard.Api.Repositories.Interfaces;

public interface IMetricRepository
{
    Task<IReadOnlyList<EntityWithServiceName<Metric>>> GetAllWithServiceAsync(CancellationToken cancellationToken = default);
    Task<EntityWithServiceName<Metric>?> GetByIdWithServiceAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EntityWithServiceName<Metric>>> GetByServiceIdWithServiceAsync(
        int serviceId,
        CancellationToken cancellationToken = default);
    Task AddAsync(Metric metric, CancellationToken cancellationToken = default);
}
