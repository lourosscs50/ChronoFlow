namespace ChronoFlow.Modules.Events.Application;

public sealed class GetStreamEventsHandler
{
    private readonly IEventRepository _repository;

    public GetStreamEventsHandler(IEventRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<GetStreamEventResult>> HandleAsync(
        GetStreamEventsQuery query,
        CancellationToken cancellationToken)
    {
        var events = await _repository.ListByStreamIdAsync(query.StreamId, cancellationToken);

        return events
            .OrderBy(x => x.OccurredAtUtc)
            .Select(x => new GetStreamEventResult(
                x.Id,
                x.StreamId,
                x.EventType,
                x.Payload,
                x.OccurredAtUtc,
                x.CreatedByUserId))
            .ToList();
    }
}