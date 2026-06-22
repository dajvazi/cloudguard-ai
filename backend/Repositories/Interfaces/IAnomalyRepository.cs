using CloudGuard.Api.Models;
using CloudGuard.Api.Repositories.ReadModels;

namespace CloudGuard.Api.Repositories.Interfaces;

public interface IAnomalyRepository
{
    Task<IReadOnlyList<EntityWithServiceName<Anomaly>>> GetAllWithServiceAsync(CancellationToken cancellationToken = default);
    Task<EntityWithServiceName<Anomaly>?> GetByIdWithServiceAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EntityWithServiceName<Anomaly>>> GetByServiceIdWithServiceAsync(
        int serviceId,
        CancellationToken cancellationToken = default);
    Task AddAsync(Anomaly anomaly, CancellationToken cancellationToken = default);
}
