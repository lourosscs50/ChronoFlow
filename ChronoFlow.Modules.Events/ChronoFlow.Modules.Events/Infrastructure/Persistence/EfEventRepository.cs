using ChronoFlow.Modules.Events.Application;
using ChronoFlow.Modules.Events.Domain;
using Microsoft.EntityFrameworkCore;

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
        _dbContext.Events.Add(eventRecord);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<EventRecord?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken)
    {
        return await _dbContext.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == eventId, cancellationToken);
    }

    public async Task<IReadOnlyList<EventRecord>> ListByStreamIdAsync(string streamId, CancellationToken cancellationToken)
    {
        return await _dbContext.Events
            .AsNoTracking()
            .Where(x => x.StreamId == streamId)
            .OrderBy(x => x.OccurredAtUtc)
            .ToListAsync(cancellationToken);
    }
}