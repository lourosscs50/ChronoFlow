namespace ChronoFlow.Modules.Events.Application;

public sealed record GetEventResult(
    Guid EventId,
    string StreamId,
    string EventType,
    string Payload,
    DateTime OccurredAtUtc,
    Guid CreatedByUserId);