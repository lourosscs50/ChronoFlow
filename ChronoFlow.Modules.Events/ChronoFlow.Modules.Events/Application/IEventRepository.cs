using ChronoFlow.Modules.Events.Domain;

namespace ChronoFlow.Modules.Events.Application;

public interface IEventRepository
{
    Task AddAsync(EventRecord eventRecord, CancellationToken cancellationToken);
}