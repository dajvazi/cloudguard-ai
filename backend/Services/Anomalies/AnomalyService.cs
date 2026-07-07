using CloudGuard.Api.DTOs;
using CloudGuard.Api.DTOs.Requests;
using CloudGuard.Api.Mappings;
using CloudGuard.Api.Models;
using CloudGuard.Api.Repositories.Interfaces;

namespace CloudGuard.Api.Services.Anomalies;

public class AnomalyService(
    IAnomalyRepository anomalyRepository,
    ICloudServiceRepository cloudServiceRepository,
    IUnitOfWork unitOfWork) : IAnomalyService
{
    public async Task<IReadOnlyList<AnomalyDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var results = await anomalyRepository.GetAllWithServiceAsync(cancellationToken);
        return results.Select(a => a.Entity.ToDto(a.ServiceName)).ToList();
    }

    public async Task<AnomalyDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await anomalyRepository.GetByIdWithServiceAsync(id, cancellationToken);
        return result is null ? null : result.Entity.ToDto(result.ServiceName);
    }

    public async Task<IReadOnlyList<AnomalyDto>> GetByServiceIdAsync(
        int serviceId,
        CancellationToken cancellationToken = default)
    {
        var results = await anomalyRepository.GetByServiceIdWithServiceAsync(serviceId, cancellationToken);
        return results.Select(a => a.Entity.ToDto(a.ServiceName)).ToList();
    }

    public async Task<AnomalyDto> CreateAsync(
        CreateAnomalyRequest request,
        CancellationToken cancellationToken = default)
    {
        var service = await cloudServiceRepository.GetByIdAsync(request.CloudServiceId, cancellationToken)
            ?? throw new ArgumentException($"Service with id {request.CloudServiceId} was not found.");

        var anomaly = new Anomaly
        {
            CloudServiceId = request.CloudServiceId,
            AnomalyType = request.AnomalyType,
            Severity = request.Severity,
            AiConfidence = request.AiConfidence,
            Description = request.Description,
            DetectedAt = DateTime.UtcNow,
        };

        await anomalyRepository.AddAsync(anomaly, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return anomaly.ToDto(service.Name);
    }
}
