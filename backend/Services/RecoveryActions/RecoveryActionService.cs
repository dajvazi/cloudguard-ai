using CloudGuard.Api.Constants;
using CloudGuard.Api.DTOs;
using CloudGuard.Api.DTOs.Requests;
using CloudGuard.Api.Mappings;
using CloudGuard.Api.Models;
using CloudGuard.Api.Repositories.Interfaces;

namespace CloudGuard.Api.Services.RecoveryActions;

public class RecoveryActionService(
    IRecoveryActionRepository recoveryActionRepository,
    IIncidentRepository incidentRepository,
    IUnitOfWork unitOfWork) : IRecoveryActionService
{
    private static readonly HashSet<string> ValidStatuses =
    [
        RecoveryActionStatus.Pending,
        RecoveryActionStatus.InProgress,
        RecoveryActionStatus.Completed,
        RecoveryActionStatus.Failed,
    ];

    public async Task<IReadOnlyList<RecoveryActionDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var actions = await recoveryActionRepository.GetAllAsync(cancellationToken);
        return actions.Select(a => a.ToDto()).ToList();
    }

    public async Task<RecoveryActionDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var action = await recoveryActionRepository.GetByIdAsync(id, cancellationToken);
        return action?.ToDto();
    }

    public async Task<IReadOnlyList<RecoveryActionDto>> GetByIncidentIdAsync(
        int incidentId,
        CancellationToken cancellationToken = default)
    {
        var actions = await recoveryActionRepository.GetByIncidentIdAsync(incidentId, cancellationToken);
        return actions.Select(a => a.ToDto()).ToList();
    }

    public async Task<RecoveryActionDto> CreateAsync(
        CreateRecoveryActionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await incidentRepository.ExistsAsync(request.IncidentId, cancellationToken))
            throw new ArgumentException($"Incidenti me id {request.IncidentId} nuk u gjet.");

        var action = new RecoveryAction
        {
            IncidentId = request.IncidentId,
            ActionType = request.ActionType,
            ActionStatus = RecoveryActionStatus.Pending,
            Description = request.Description,
            ExecutedAt = DateTime.UtcNow,
        };

        await recoveryActionRepository.AddAsync(action, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return action.ToDto();
    }

    public async Task<RecoveryActionDto?> UpdateStatusAsync(
        int id,
        string actionStatus,
        CancellationToken cancellationToken = default)
    {
        if (!ValidStatuses.Contains(actionStatus))
            throw new ArgumentException($"Status i pavlefshëm: {actionStatus}");

        var action = await recoveryActionRepository.GetByIdForUpdateAsync(id, cancellationToken);
        if (action is null)
            return null;

        action.ActionStatus = actionStatus;
        action.ExecutedAt = DateTime.UtcNow;

        if (actionStatus is RecoveryActionStatus.Completed or RecoveryActionStatus.InProgress)
            await ApplyIncidentSideEffectsAsync(action.IncidentId, actionStatus, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return action.ToDto();
    }

    public async Task<RecoveryActionDto?> ExecuteAsync(int id, CancellationToken cancellationToken = default) =>
        await UpdateStatusAsync(id, RecoveryActionStatus.Completed, cancellationToken);

    private async Task ApplyIncidentSideEffectsAsync(
        int incidentId,
        string actionStatus,
        CancellationToken cancellationToken)
    {
        var incident = await incidentRepository.GetByIdWithServiceForUpdateAsync(incidentId, cancellationToken);
        if (incident is null)
            return;

        if (actionStatus == RecoveryActionStatus.InProgress)
            incident.Status = IncidentStatus.Mitigating;

        if (actionStatus == RecoveryActionStatus.Completed)
        {
            incident.Status = IncidentStatus.Resolved;
            incident.ResolvedAt = DateTime.UtcNow;
            incident.CloudService.Status = ServiceStatus.Healthy;
        }
    }
}
