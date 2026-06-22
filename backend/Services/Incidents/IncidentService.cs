using CloudGuard.Api.Constants;
using CloudGuard.Api.DTOs;
using CloudGuard.Api.DTOs.Requests;
using CloudGuard.Api.Mappings;
using CloudGuard.Api.Models;
using CloudGuard.Api.Repositories.Interfaces;

namespace CloudGuard.Api.Services.Incidents;

public class IncidentService(
    IIncidentRepository incidentRepository,
    ICloudServiceRepository cloudServiceRepository,
    IUnitOfWork unitOfWork) : IIncidentService
{
    private static readonly HashSet<string> ValidStatuses =
    [
        IncidentStatus.Open,
        IncidentStatus.Investigating,
        IncidentStatus.Mitigating,
        IncidentStatus.Resolved,
    ];

    public async Task<IReadOnlyList<IncidentDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var results = await incidentRepository.GetAllWithServiceAsync(cancellationToken);
        return results.Select(i => i.Entity.ToDto(i.ServiceName)).ToList();
    }

    public async Task<IReadOnlyList<IncidentDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var results = await incidentRepository.GetActiveWithServiceAsync(cancellationToken);
        return results.Select(i => i.Entity.ToDto(i.ServiceName)).ToList();
    }

    public async Task<IncidentDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var incident = await incidentRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        return incident?.ToDetailDto(incident.CloudService.Name);
    }

    public async Task<IReadOnlyList<IncidentDto>> GetByServiceIdAsync(
        int serviceId,
        CancellationToken cancellationToken = default)
    {
        var results = await incidentRepository.GetByServiceIdWithServiceAsync(serviceId, cancellationToken);
        return results.Select(i => i.Entity.ToDto(i.ServiceName)).ToList();
    }

    public async Task<IncidentDto> CreateAsync(
        CreateIncidentRequest request,
        CancellationToken cancellationToken = default)
    {
        var service = await cloudServiceRepository.GetByIdAsync(request.CloudServiceId, cancellationToken)
            ?? throw new ArgumentException($"Shërbimi me id {request.CloudServiceId} nuk u gjet.");

        var incident = new Incident
        {
            CloudServiceId = request.CloudServiceId,
            Title = request.Title,
            Severity = request.Severity,
            Status = IncidentStatus.Open,
            RootCause = request.RootCause,
            CreatedAt = DateTime.UtcNow,
        };

        await incidentRepository.AddAsync(incident, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return incident.ToDto(service.Name);
    }

    public async Task<IncidentDto?> UpdateStatusAsync(
        int id,
        string status,
        CancellationToken cancellationToken = default)
    {
        if (!ValidStatuses.Contains(status))
            throw new ArgumentException($"Status i pavlefshëm: {status}");

        var incident = await incidentRepository.GetByIdWithServiceForUpdateAsync(id, cancellationToken);
        if (incident is null)
            return null;

        incident.Status = status;

        if (status == IncidentStatus.Resolved)
            incident.ResolvedAt = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return incident.ToDto(incident.CloudService.Name);
    }

    public async Task<IncidentDto?> ResolveAsync(int id, CancellationToken cancellationToken = default) =>
        await UpdateStatusAsync(id, IncidentStatus.Resolved, cancellationToken);
}
