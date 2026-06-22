using CloudGuard.Api.Models;
using CloudGuard.Api.Repositories.ReadModels;

namespace CloudGuard.Api.Repositories.Interfaces;

public interface IIncidentRepository
{
    Task<IReadOnlyList<EntityWithServiceName<Incident>>> GetAllWithServiceAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EntityWithServiceName<Incident>>> GetActiveWithServiceAsync(CancellationToken cancellationToken = default);
    Task<Incident?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EntityWithServiceName<Incident>>> GetByServiceIdWithServiceAsync(
        int serviceId,
        CancellationToken cancellationToken = default);
    Task<Incident?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken = default);
    Task<Incident?> GetByIdWithServiceForUpdateAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(Incident incident, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
}
