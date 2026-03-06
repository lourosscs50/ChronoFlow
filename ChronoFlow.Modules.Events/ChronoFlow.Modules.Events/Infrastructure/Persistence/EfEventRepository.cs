using ChronoFlow.Modules.Events.Application;
using ChronoFlow.Modules.Events.Domain;

namespace ChronoFlow.Modules.Events.Infrastructure.Persistence;

public sealed class EfEventRepository : IEventRepository
{
    private readonly EventsDbContext _dbContext;

    public EfEventRepository(EventsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(EventRecord eventRecord, CancellationToken cancellationToken)
    {
        await _dbContext.Events.AddAsync(eventRecord, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}