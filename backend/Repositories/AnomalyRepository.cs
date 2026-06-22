using CloudGuard.Api.Data;
using CloudGuard.Api.Models;
using CloudGuard.Api.Repositories.Interfaces;
using CloudGuard.Api.Repositories.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace CloudGuard.Api.Repositories;

public class AnomalyRepository(CloudGuardDbContext dbContext) : IAnomalyRepository
{
    public async Task<IReadOnlyList<EntityWithServiceName<Anomaly>>> GetAllWithServiceAsync(
        CancellationToken cancellationToken = default)
    {
        var results = await QueryWithService()
            .OrderByDescending(a => a.Entity.DetectedAt)
            .ToListAsync(cancellationToken);

        return results;
    }

    public async Task<EntityWithServiceName<Anomaly>?> GetByIdWithServiceAsync(
        int id,
        CancellationToken cancellationToken = default) =>
        await QueryWithService()
            .FirstOrDefaultAsync(a => a.Entity.Id == id, cancellationToken);

    public async Task<IReadOnlyList<EntityWithServiceName<Anomaly>>> GetByServiceIdWithServiceAsync(
        int serviceId,
        CancellationToken cancellationToken = default)
    {
        var results = await QueryWithService()
            .Where(a => a.Entity.CloudServiceId == serviceId)
            .OrderByDescending(a => a.Entity.DetectedAt)
            .ToListAsync(cancellationToken);

        return results;
    }

    public async Task AddAsync(Anomaly anomaly, CancellationToken cancellationToken = default) =>
        await dbContext.Anomalies.AddAsync(anomaly, cancellationToken);

    private IQueryable<EntityWithServiceName<Anomaly>> QueryWithService() =>
        from anomaly in dbContext.Anomalies.AsNoTracking()
        join service in dbContext.CloudServices.AsNoTracking()
            on anomaly.CloudServiceId equals service.Id
        select new EntityWithServiceName<Anomaly>(anomaly, service.Name);
}
