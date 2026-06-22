using CloudGuard.Api.Data;
using CloudGuard.Api.Models;
using CloudGuard.Api.Repositories.Interfaces;
using CloudGuard.Api.Repositories.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace CloudGuard.Api.Repositories;

public class MetricRepository(CloudGuardDbContext dbContext) : IMetricRepository
{
    public async Task<IReadOnlyList<EntityWithServiceName<Metric>>> GetAllWithServiceAsync(
        CancellationToken cancellationToken = default)
    {
        var results = await QueryWithService()
            .OrderByDescending(m => m.Entity.RecordedAt)
            .ToListAsync(cancellationToken);

        return results;
    }

    public async Task<EntityWithServiceName<Metric>?> GetByIdWithServiceAsync(
        int id,
        CancellationToken cancellationToken = default) =>
        await QueryWithService()
            .FirstOrDefaultAsync(m => m.Entity.Id == id, cancellationToken);

    public async Task<IReadOnlyList<EntityWithServiceName<Metric>>> GetByServiceIdWithServiceAsync(
        int serviceId,
        CancellationToken cancellationToken = default)
    {
        var results = await QueryWithService()
            .Where(m => m.Entity.CloudServiceId == serviceId)
            .OrderByDescending(m => m.Entity.RecordedAt)
            .ToListAsync(cancellationToken);

        return results;
    }

    public async Task AddAsync(Metric metric, CancellationToken cancellationToken = default) =>
        await dbContext.Metrics.AddAsync(metric, cancellationToken);

    private IQueryable<EntityWithServiceName<Metric>> QueryWithService() =>
        from metric in dbContext.Metrics.AsNoTracking()
        join service in dbContext.CloudServices.AsNoTracking()
            on metric.CloudServiceId equals service.Id
        select new EntityWithServiceName<Metric>(metric, service.Name);
}
