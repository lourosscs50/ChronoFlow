using ChronoFlow.Modules.Events.Domain;

namespace ChronoFlow.Modules.Events.Application;

public sealed class GetEventHandler
{
    private readonly IEventRepository _repository;

    public GetEventHandler(IEventRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetEventResult?> HandleAsync(
        GetEventQuery query,
        CancellationToken cancellationToken)
    {
        EventRecord? eventRecord = await _repository.GetByIdAsync(query.EventId, cancellationToken);

        if (eventRecord is null)
        {
            return null;
        }

        return new GetEventResult(
            eventRecord.Id,
            eventRecord.StreamId,
            eventRecord.EventType,
            eventRecord.Payload,
            eventRecord.OccurredAtUtc,
            eventRecord.CreatedByUserId);
    }
}