using CloudGuard.Api.DTOs;
using CloudGuard.Api.DTOs.Requests;

namespace CloudGuard.Api.Services.Incidents;

public interface IIncidentService
{
    Task<IReadOnlyList<IncidentDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IncidentDto>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<IncidentDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IncidentDto>> GetByServiceIdAsync(int serviceId, CancellationToken cancellationToken = default);
    Task<IncidentDto> CreateAsync(CreateIncidentRequest request, CancellationToken cancellationToken = default);
    Task<IncidentDto?> UpdateStatusAsync(int id, string status, CancellationToken cancellationToken = default);
    Task<IncidentDto?> ResolveAsync(int id, CancellationToken cancellationToken = default);
}
