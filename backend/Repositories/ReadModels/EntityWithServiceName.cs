namespace CloudGuard.Api.Repositories.ReadModels;

public record EntityWithServiceName<TEntity>(TEntity Entity, string ServiceName);
