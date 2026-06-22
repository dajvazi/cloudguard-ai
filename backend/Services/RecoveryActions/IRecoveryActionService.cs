using CloudGuard.Api.DTOs;
using CloudGuard.Api.DTOs.Requests;

namespace CloudGuard.Api.Services.RecoveryActions;

public interface IRecoveryActionService
{
    Task<IReadOnlyList<RecoveryActionDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<RecoveryActionDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecoveryActionDto>> GetByIncidentIdAsync(int incidentId, CancellationToken cancellationToken = default);
    Task<RecoveryActionDto> CreateAsync(CreateRecoveryActionRequest request, CancellationToken cancellationToken = default);
    Task<RecoveryActionDto?> UpdateStatusAsync(int id, string actionStatus, CancellationToken cancellationToken = default);
    Task<RecoveryActionDto?> ExecuteAsync(int id, CancellationToken cancellationToken = default);
}
