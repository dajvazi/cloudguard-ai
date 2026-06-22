using CloudGuard.Api.Constants;
using CloudGuard.Api.Data;
using CloudGuard.Api.Models;
using CloudGuard.Api.Repositories.Interfaces;
using CloudGuard.Api.Repositories.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace CloudGuard.Api.Repositories;

public class IncidentRepository(CloudGuardDbContext dbContext) : IIncidentRepository
{
    public async Task<IReadOnlyList<EntityWithServiceName<Incident>>> GetAllWithServiceAsync(
        CancellationToken cancellationToken = default)
    {
        var results = await JoinQuery()
            .OrderByDescending(x => x.incident.CreatedAt)
            .ToListAsync(cancellationToken);

        return ToEntities(results);
    }

    public async Task<IReadOnlyList<EntityWithServiceName<Incident>>> GetActiveWithServiceAsync(
        CancellationToken cancellationToken = default)
    {
        var results = await JoinQuery()
            .Where(x => x.incident.Status != IncidentStatus.Resolved)
            .OrderByDescending(x => x.incident.CreatedAt)
            .ToListAsync(cancellationToken);

        return ToEntities(results);
    }

    public async Task<Incident?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default) =>
        await dbContext.Incidents
            .AsNoTracking()
            .Include(i => i.CloudService)
            .Include(i => i.RecoveryActions)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task<IReadOnlyList<EntityWithServiceName<Incident>>> GetByServiceIdWithServiceAsync(
        int serviceId,
        CancellationToken cancellationToken = default)
    {
        var results = await JoinQuery()
            .Where(x => x.incident.CloudServiceId == serviceId)
            .OrderByDescending(x => x.incident.CreatedAt)
            .ToListAsync(cancellationToken);

        return ToEntities(results);
    }

    public async Task<Incident?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken = default) =>
        await dbContext.Incidents.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task<Incident?> GetByIdWithServiceForUpdateAsync(int id, CancellationToken cancellationToken = default) =>
        await dbContext.Incidents
            .Include(i => i.CloudService)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task AddAsync(Incident incident, CancellationToken cancellationToken = default) =>
        await dbContext.Incidents.AddAsync(incident, cancellationToken);

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        await dbContext.Incidents.AnyAsync(i => i.Id == id, cancellationToken);

    private IQueryable<IncidentJoinRow> JoinQuery() =>
        from incident in dbContext.Incidents.AsNoTracking()
        join service in dbContext.CloudServices.AsNoTracking()
            on incident.CloudServiceId equals service.Id
        select new IncidentJoinRow { incident = incident, serviceName = service.Name };

    private static List<EntityWithServiceName<Incident>> ToEntities(List<IncidentJoinRow> rows) =>
        rows.Select(x => new EntityWithServiceName<Incident>(x.incident, x.serviceName)).ToList();

    private sealed class IncidentJoinRow
    {
        public Incident incident { get; init; } = null!;
        public string serviceName { get; init; } = string.Empty;
    }
}
