namespace CloudGuard.Api.DTOs;

public record ResourceDto(
    int Id,
    string ResourceName,
    string ResourceType,
    string? Source,
    string Status,
    DateTime DiscoveredAt);
