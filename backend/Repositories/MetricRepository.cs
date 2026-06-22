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
        var results = await JoinQuery()
            .OrderByDescending(x => x.metric.RecordedAt)
            .ToListAsync(cancellationToken);

        return ToEntities(results);
    }

    public async Task<EntityWithServiceName<Metric>?> GetByIdWithServiceAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var result = await JoinQuery()
            .FirstOrDefaultAsync(x => x.metric.Id == id, cancellationToken);

        return result is null ? null : new EntityWithServiceName<Metric>(result.metric, result.serviceName);
    }

    public async Task<IReadOnlyList<EntityWithServiceName<Metric>>> GetByServiceIdWithServiceAsync(
        int serviceId,
        CancellationToken cancellationToken = default)
    {
        var results = await JoinQuery()
            .Where(x => x.metric.CloudServiceId == serviceId)
            .OrderByDescending(x => x.metric.RecordedAt)
            .ToListAsync(cancellationToken);

        return ToEntities(results);
    }

    public async Task AddAsync(Metric metric, CancellationToken cancellationToken = default) =>
        await dbContext.Metrics.AddAsync(metric, cancellationToken);

    private IQueryable<MetricJoinRow> JoinQuery() =>
        from metric in dbContext.Metrics.AsNoTracking()
        join service in dbContext.CloudServices.AsNoTracking()
            on metric.CloudServiceId equals service.Id
        select new MetricJoinRow { metric = metric, serviceName = service.Name };

    private static List<EntityWithServiceName<Metric>> ToEntities(List<MetricJoinRow> rows) =>
        rows.Select(x => new EntityWithServiceName<Metric>(x.metric, x.serviceName)).ToList();

    private sealed class MetricJoinRow
    {
        public Metric metric { get; init; } = null!;
        public string serviceName { get; init; } = string.Empty;
    }
}
