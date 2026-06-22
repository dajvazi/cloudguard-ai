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
        var results = await JoinQuery()
            .OrderByDescending(x => x.anomaly.DetectedAt)
            .ToListAsync(cancellationToken);

        return ToEntities(results);
    }

    public async Task<EntityWithServiceName<Anomaly>?> GetByIdWithServiceAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var result = await JoinQuery()
            .FirstOrDefaultAsync(x => x.anomaly.Id == id, cancellationToken);

        return result is null ? null : new EntityWithServiceName<Anomaly>(result.anomaly, result.serviceName);
    }

    public async Task<IReadOnlyList<EntityWithServiceName<Anomaly>>> GetByServiceIdWithServiceAsync(
        int serviceId,
        CancellationToken cancellationToken = default)
    {
        var results = await JoinQuery()
            .Where(x => x.anomaly.CloudServiceId == serviceId)
            .OrderByDescending(x => x.anomaly.DetectedAt)
            .ToListAsync(cancellationToken);

        return ToEntities(results);
    }

    public async Task AddAsync(Anomaly anomaly, CancellationToken cancellationToken = default) =>
        await dbContext.Anomalies.AddAsync(anomaly, cancellationToken);

    private IQueryable<AnomalyJoinRow> JoinQuery() =>
        from anomaly in dbContext.Anomalies.AsNoTracking()
        join service in dbContext.CloudServices.AsNoTracking()
            on anomaly.CloudServiceId equals service.Id
        select new AnomalyJoinRow { anomaly = anomaly, serviceName = service.Name };

    private static List<EntityWithServiceName<Anomaly>> ToEntities(List<AnomalyJoinRow> rows) =>
        rows.Select(x => new EntityWithServiceName<Anomaly>(x.anomaly, x.serviceName)).ToList();

    private sealed class AnomalyJoinRow
    {
        public Anomaly anomaly { get; init; } = null!;
        public string serviceName { get; init; } = string.Empty;
    }
}
