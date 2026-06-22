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
        var results = await QueryWithService()
            .OrderByDescending(i => i.Entity.CreatedAt)
            .ToListAsync(cancellationToken);

        return results;
    }

    public async Task<IReadOnlyList<EntityWithServiceName<Incident>>> GetActiveWithServiceAsync(
        CancellationToken cancellationToken = default)
    {
        var results = await QueryWithService()
            .Where(i => i.Entity.Status != Constants.IncidentStatus.Resolved)
            .OrderByDescending(i => i.Entity.CreatedAt)
            .ToListAsync(cancellationToken);

        return results;
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
        var results = await QueryWithService()
            .Where(i => i.Entity.CloudServiceId == serviceId)
            .OrderByDescending(i => i.Entity.CreatedAt)
            .ToListAsync(cancellationToken);

        return results;
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

    private IQueryable<EntityWithServiceName<Incident>> QueryWithService() =>
        from incident in dbContext.Incidents.AsNoTracking()
        join service in dbContext.CloudServices.AsNoTracking()
            on incident.CloudServiceId equals service.Id
        select new EntityWithServiceName<Incident>(incident, service.Name);
}
