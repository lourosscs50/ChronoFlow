namespace ChronoFlow.Modules.Events.Application;

public sealed record IngestEventCommand(
    string StreamId,
    string EventType,
    string Payload,
    DateTime? OccurredAtUtc,
    Guid CreatedByUserId
);