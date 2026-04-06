namespace ChronoFlow.Api.Contracts.Control;

/// <summary>JSON shape aligned with SignalForge control automation trigger contract.</summary>
public sealed record ReceiveControlTriggerRequest(
    string TriggerType,
    Guid AlertId,
    Guid RuleId,
    Guid SignalId,
    DateTimeOffset OccurredAtUtc,
    string CurrentStatus,
    string LifecycleEventType,
    string? AcknowledgedByUserId,
    string? ResolvedByUserId,
    string? ReopenedByUserId,
    string? RuleName,
    bool HasBeenReopened,
    string? CorrelationId = null);
