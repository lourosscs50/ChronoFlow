using ChronoFlow.Modules.Events.Domain;

namespace ChronoFlow.Modules.Events.Application;

public sealed class IngestEventHandler
{
    private readonly IEventRepository _eventRepository;

    public IngestEventHandler(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository;
    }

    public async Task<IngestEventResult> HandleAsync(
        IngestEventCommand command,
        CancellationToken cancellationToken)
    {
        var occurredAtUtc = command.OccurredAtUtc ?? DateTime.UtcNow;

        var eventRecord = EventRecord.Create(
            command.StreamId,
            command.EventType,
            command.Payload,
            occurredAtUtc,
            command.CreatedByUserId);

        await _eventRepository.AddAsync(eventRecord, cancellationToken);

        return new IngestEventResult(eventRecord.Id);
    }
}