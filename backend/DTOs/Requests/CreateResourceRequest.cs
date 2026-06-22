namespace CloudGuard.Api.DTOs.Requests;

public record CreateResourceRequest(
    string ResourceName,
    string ResourceType,
    string? Source,
    string? Status);
