namespace ChronoFlow.Api.Contracts.Events;

public sealed record GetStreamEventItemResponse(
    Guid EventId,
    string StreamId,
    string EventType,
    string Payload,
    DateTime OccurredAtUtc,
    Guid CreatedByUserId);