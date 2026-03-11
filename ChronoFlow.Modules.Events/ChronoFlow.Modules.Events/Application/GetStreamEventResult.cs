namespace ChronoFlow.Modules.Events.Application;

public sealed record GetStreamEventResult(
    Guid EventId,
    string StreamId,
    string EventType,
    string Payload,
    DateTime OccurredAtUtc,
    Guid CreatedByUserId);