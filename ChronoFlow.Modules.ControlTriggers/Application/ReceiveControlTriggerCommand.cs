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
    bool HasBeenReopened,
    /// <summary>Optional trace correlation from upstream intake (bounded, operator-safe).</summary>
    string? CorrelationId = null,
    /// <summary>Optional structured decision/advisory context from upstream (bounded; snapshotted at start, not recomputed).</summary>
    InboundDecisionContext? InboundDecision = null);
