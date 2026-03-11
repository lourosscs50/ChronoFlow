using ChronoFlow.Modules.Events.Domain;

namespace ChronoFlow.Modules.Events.Application;

public interface IEventRepository
{
    Task AddAsync(EventRecord eventRecord, CancellationToken cancellationToken);

    Task<EventRecord?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken);

    Task<IReadOnlyList<EventRecord>> ListByStreamIdAsync(string streamId, CancellationToken cancellationToken);
}