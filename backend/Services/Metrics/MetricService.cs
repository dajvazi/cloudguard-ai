using CloudGuard.Api.DTOs;
using CloudGuard.Api.DTOs.Requests;
using CloudGuard.Api.Mappings;
using CloudGuard.Api.Models;
using CloudGuard.Api.Repositories.Interfaces;

namespace CloudGuard.Api.Services.Metrics;

public class MetricService(
    IMetricRepository metricRepository,
    ICloudServiceRepository cloudServiceRepository,
    IUnitOfWork unitOfWork) : IMetricService
{
    public async Task<IReadOnlyList<MetricDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var results = await metricRepository.GetAllWithServiceAsync(cancellationToken);
        return results.Select(m => m.Entity.ToDto(m.ServiceName)).ToList();
    }

    public async Task<MetricDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await metricRepository.GetByIdWithServiceAsync(id, cancellationToken);
        return result is null ? null : result.Entity.ToDto(result.ServiceName);
    }

    public async Task<IReadOnlyList<MetricDto>> GetByServiceIdAsync(
        int serviceId,
        CancellationToken cancellationToken = default)
    {
        var results = await metricRepository.GetByServiceIdWithServiceAsync(serviceId, cancellationToken);
        return results.Select(m => m.Entity.ToDto(m.ServiceName)).ToList();
    }

    public async Task<MetricDto> CreateAsync(
        CreateMetricRequest request,
        CancellationToken cancellationToken = default)
    {
        var service = await cloudServiceRepository.GetByIdAsync(request.CloudServiceId, cancellationToken)
            ?? throw new ArgumentException($"Service with id {request.CloudServiceId} was not found.");

        var metric = new Metric
        {
            CloudServiceId = request.CloudServiceId,
            CpuUsage = request.CpuUsage,
            MemoryUsage = request.MemoryUsage,
            LatencyMs = request.LatencyMs,
            ErrorRate = request.ErrorRate,
            RecordedAt = DateTime.UtcNow,
        };

        await metricRepository.AddAsync(metric, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return metric.ToDto(service.Name);
    }
}
