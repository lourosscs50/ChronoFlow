namespace ChronoFlow.Modules.Events.Domain;

public sealed class EventRecord
{
    public Guid Id { get; private set; }
    public string StreamId { get; private set; }
    public string EventType { get; private set; }
    public string Payload { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    private EventRecord()
    {
        StreamId = string.Empty;
        EventType = string.Empty;
        Payload = string.Empty;
    }

    private EventRecord(
        Guid id,
        string streamId,
        string eventType,
        string payload,
        DateTime occurredAtUtc,
        Guid createdByUserId)
    {
        Id = id;
        StreamId = streamId;
        EventType = eventType;
        Payload = payload;
        OccurredAtUtc = occurredAtUtc;
        CreatedByUserId = createdByUserId;
    }

    public static EventRecord Create(
        string streamId,
        string eventType,
        string payload,
        DateTime occurredAtUtc,
        Guid createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(streamId))
            throw new ArgumentException("StreamId is required.", nameof(streamId));

        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("EventType is required.", nameof(eventType));

        if (string.IsNullOrWhiteSpace(payload))
            throw new ArgumentException("Payload is required.", nameof(payload));

        if (createdByUserId == Guid.Empty)
            throw new ArgumentException("CreatedByUserId is required.", nameof(createdByUserId));

        return new EventRecord(
            Guid.NewGuid(),
            streamId.Trim(),
            eventType.Trim(),
            payload.Trim(),
            occurredAtUtc,
            createdByUserId);
    }
}