namespace ChronoFlow.Modules.ControlTriggers.Application;

/// <summary>
/// Bounded, policy-safe view of trigger-time facts (no command references, no infrastructure types).
/// </summary>
public sealed record WorkflowPolicyInput(
    string TriggerType,
    string LifecycleEventType,
    string CurrentStatus,
    Guid AlertId,
    Guid RuleId,
    Guid SignalId,
    bool HasBeenReopened,
    bool AdvisoryWasUsed,
    string? AdvisoryStrategyKey,
    string? AdvisoryConfidence,
    string? AdvisoryReasonSummary,
    string? InboundDecisionSummary,
    string? InboundDecisionReferenceId,
    string? InboundDecisionConfidence,
    string? InboundDecisionReasonCode,
    string? InboundLinkedExternalExecutionId,
    /// <summary>Workflow key that would execute if policy proceeds; null when no definition.</summary>
    string? ResolvedWorkflowKey)
{
    public static WorkflowPolicyInput From(
        ReceiveControlTriggerCommand command,
        AdvisoryExecutionSnapshot snapshot,
        string? resolvedWorkflowKey) =>
        new(
            command.TriggerType,
            command.LifecycleEventType,
            command.CurrentStatus,
            command.AlertId,
            command.RuleId,
            command.SignalId,
            command.HasBeenReopened,
            snapshot.AdvisoryWasUsed,
            snapshot.AdvisoryStrategyKey,
            snapshot.AdvisoryConfidence,
            snapshot.AdvisoryReasonSummary,
            snapshot.InboundDecisionSummary,
            snapshot.InboundDecisionReferenceId,
            snapshot.InboundDecisionConfidence,
            snapshot.InboundDecisionReasonCode,
            snapshot.InboundLinkedExternalExecutionId,
            resolvedWorkflowKey);
}
