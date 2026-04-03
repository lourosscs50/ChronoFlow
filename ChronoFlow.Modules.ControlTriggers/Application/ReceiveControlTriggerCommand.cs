namespace ChronoFlow.Modules.ControlTriggers.Application;

/// <summary>Normalized control trigger intake aligned with SignalForge <c>ControlAutomationTriggerRequest</c> (no workflow identifiers).</summary>
public sealed record ReceiveControlTriggerCommand(
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
    bool HasBeenReopened);
