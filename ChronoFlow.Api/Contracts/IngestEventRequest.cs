namespace ChronoFlow.Api.Contracts.Events;

public sealed record IngestEventRequest(
    string StreamId,
    string EventType,
    string Payload,
    DateTime? OccurredAtUtc
);