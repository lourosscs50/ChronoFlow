namespace ChronoFlow.Modules.ControlTriggers.Application;

/// <summary>Operator-safe payload for a single A.I.L. advisory call (no prompts or model internals).</summary>
public sealed record AdvisoryExecutionRequest(
    string TriggerType,
    Guid AlertId,
    Guid RuleId,
    Guid SignalId,
    string CurrentStatus,
    string LifecycleEventType,
    string? RuleName,
    bool HasBeenReopened,
    /// <summary>Optional upstream correlation (e.g. intake or platform trace id).</summary>
    string? CorrelationId,
    /// <summary>ChronoFlow orchestration identity for this attempt; aligns with workflow execution instance id when execution proceeds.</summary>
    Guid OrchestrationExecutionInstanceId);
