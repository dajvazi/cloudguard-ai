using CloudGuard.Api.Models;

namespace CloudGuard.Api.Repositories.Interfaces;

public interface IRecoveryActionRepository
{
    Task<IReadOnlyList<RecoveryAction>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<RecoveryAction?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecoveryAction>> GetByIncidentIdAsync(int incidentId, CancellationToken cancellationToken = default);
    Task<RecoveryAction?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(RecoveryAction action, CancellationToken cancellationToken = default);
}
