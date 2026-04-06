namespace ChronoFlow.Modules.ControlTriggers.Domain;

/// <summary>Durable record of an accepted control-trigger intake outcome (operator visibility).</summary>
public sealed class ControlExecutionRecord
{
    public Guid Id { get; init; }

    /// <summary>Authoritative id for a single workflow execution instance; set only when execution starts; distinct from <see cref="Id"/>.</summary>
    public Guid? ExecutionInstanceId { get; init; }
    public string TriggerType { get; init; } = "";
    public string LifecycleEventType { get; init; } = "";
    public Guid AlertId { get; init; }
    public Guid RuleId { get; init; }
    public Guid SignalId { get; init; }

    /// <summary>Intake correlation id snapshotted at acceptance; null when not supplied.</summary>
    public string? CorrelationId { get; init; }

    public string? WorkflowKey { get; init; }
    public bool WasExecuted { get; init; }
    public bool WasSuppressed { get; init; }
    public string? SuppressionReason { get; init; }
    public int ExecutedStepCount { get; init; }
    public DateTimeOffset ReceivedAtUtc { get; init; }

    /// <summary>Upstream trigger occurrence time snapshotted at acceptance; null for legacy rows (replay uses <see cref="ReceivedAtUtc"/>).</summary>
    public DateTimeOffset? OccurredAtUtc { get; init; }

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

    /// <summary>A.I.L. execution/decision identifier when returned by advisory dependency; never invented locally.</summary>
    public string? LinkedAilExecutionId { get; init; }

    /// <summary>Bounded intake decision summary snapshotted at start.</summary>
    public string? InboundDecisionSummary { get; init; }

    public string? InboundDecisionReferenceId { get; init; }

    public string? InboundDecisionConfidence { get; init; }

    public string? InboundDecisionReasonCode { get; init; }

    /// <summary>Upstream external execution identifier when honestly supplied at intake.</summary>
    public string? InboundLinkedExternalExecutionId { get; init; }

    /// <summary>When true, workflow execution is gated pending operator review (Phase 5 will consume).</summary>
    public bool PendingOperatorReview { get; init; }

    /// <summary>Orchestration policy outcome at intake (e.g. proceed, policy_suppressed); null for legacy rows or duplicate suppression path.</summary>
    public string? OrchestrationPolicyOutcome { get; init; }

    /// <summary>Operator review resolution when a pending-review record was acted on; null until Phase 5 action.</summary>
    public string? OperatorReviewAction { get; init; }

    /// <summary>UTC time of the operator review action; null until acted.</summary>
    public DateTimeOffset? OperatorReviewActionAtUtc { get; init; }

    /// <summary>Bounded optional note supplied with the review action.</summary>
    public string? OperatorReviewNote { get; init; }
}
