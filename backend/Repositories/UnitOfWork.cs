using CloudGuard.Api.Data;

namespace CloudGuard.Api.Repositories;

public class UnitOfWork(CloudGuardDbContext dbContext) : Interfaces.IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
