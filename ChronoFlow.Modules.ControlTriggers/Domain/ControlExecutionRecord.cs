namespace ChronoFlow.Modules.ControlTriggers.Domain;

/// <summary>Durable record of an accepted control-trigger intake outcome (operator visibility).</summary>
public sealed class ControlExecutionRecord
{
    public Guid Id { get; init; }
    public string TriggerType { get; init; } = "";
    public string LifecycleEventType { get; init; } = "";
    public Guid AlertId { get; init; }
    public Guid RuleId { get; init; }
    public Guid SignalId { get; init; }
    public string? WorkflowKey { get; init; }
    public bool WasExecuted { get; init; }
    public bool WasSuppressed { get; init; }
    public string? SuppressionReason { get; init; }
    public int ExecutedStepCount { get; init; }
    public DateTimeOffset ReceivedAtUtc { get; init; }
    public DateTimeOffset? ExecutedAtUtc { get; init; }
    public string CurrentStatus { get; init; } = "";
    public string? AcknowledgedByUserId { get; init; }
    public string? ResolvedByUserId { get; init; }
    public string? ReopenedByUserId { get; init; }
    public string? RuleName { get; init; }
    public bool HasBeenReopened { get; init; }

    public bool AdvisoryWasUsed { get; init; }

    public string? AdvisoryStrategyKey { get; init; }

    public string? AdvisoryConfidence { get; init; }

    /// <summary>Short summary only; not a full audit payload.</summary>
    public string? AdvisoryReasonSummary { get; init; }
}
