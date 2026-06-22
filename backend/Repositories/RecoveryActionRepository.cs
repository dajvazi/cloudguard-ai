using CloudGuard.Api.Data;
using CloudGuard.Api.Models;
using CloudGuard.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloudGuard.Api.Repositories;

public class RecoveryActionRepository(CloudGuardDbContext dbContext) : IRecoveryActionRepository
{
    public async Task<IReadOnlyList<RecoveryAction>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.RecoveryActions
            .AsNoTracking()
            .OrderByDescending(a => a.ExecutedAt)
            .ToListAsync(cancellationToken);

    public async Task<RecoveryAction?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await dbContext.RecoveryActions
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<RecoveryAction>> GetByIncidentIdAsync(
        int incidentId,
        CancellationToken cancellationToken = default) =>
        await dbContext.RecoveryActions
            .AsNoTracking()
            .Where(a => a.IncidentId == incidentId)
            .OrderByDescending(a => a.ExecutedAt)
            .ToListAsync(cancellationToken);

    public async Task<RecoveryAction?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken = default) =>
        await dbContext.RecoveryActions.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task AddAsync(RecoveryAction action, CancellationToken cancellationToken = default) =>
        await dbContext.RecoveryActions.AddAsync(action, cancellationToken);
}
