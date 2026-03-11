using ChronoFlow.Modules.Events.Application;
using ChronoFlow.Modules.Events.Domain;

namespace ChronoFlow.Tests.Application.Events;

internal sealed class InMemoryEventRepository : IEventRepository
{
    private readonly List<EventRecord> _events = new();

    public Task AddAsync(EventRecord eventRecord, CancellationToken cancellationToken)
    {
        _events.Add(eventRecord);
        return Task.CompletedTask;
    }

    public Task<EventRecord?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var result = _events.FirstOrDefault(x => x.Id == eventId);
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<EventRecord>> ListByStreamIdAsync(string streamId, CancellationToken cancellationToken)
    {
        IReadOnlyList<EventRecord> results = _events
            .Where(x => x.StreamId == streamId)
            .OrderBy(x => x.OccurredAtUtc)
            .ToList();

        return Task.FromResult(results);
    }
}