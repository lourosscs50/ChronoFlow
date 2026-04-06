using ChronoFlow.Api.Contracts.Control;
using ChronoFlow.Modules.ControlTriggers.Domain;

namespace ChronoFlow.Api.Mapping;

/// <summary>Single mapping from domain execution records to operator-safe API responses.</summary>
public static class ControlExecutionRecordResponseMapper
{
    public static ControlExecutionRecordResponse ToResponse(ControlExecutionRecord x) =>
        new(
            x.Id,
            x.TriggerType,
            x.LifecycleEventType,
            x.AlertId,
            x.RuleId,
            x.SignalId,
            x.CorrelationId,
            x.WorkflowKey,
            x.WasExecuted,
            x.WasSuppressed,
            x.SuppressionReason,
            x.ExecutedStepCount,
            x.ReceivedAtUtc,
            x.OccurredAtUtc,
            x.ExecutedAtUtc,
            x.CurrentStatus,
            x.AdvisoryWasUsed,
            x.AdvisoryStrategyKey,
            x.AdvisoryConfidence,
            x.AdvisoryReasonSummary,
            x.LinkedAilExecutionId,
            x.InboundDecisionSummary,
            x.InboundDecisionReferenceId,
            x.InboundDecisionConfidence,
            x.InboundDecisionReasonCode,
            x.InboundLinkedExternalExecutionId,
            x.PendingOperatorReview,
            x.OrchestrationPolicyOutcome,
            x.OperatorReviewAction,
            x.OperatorReviewActionAtUtc,
            x.OperatorReviewNote,
            x.ExecutionInstanceId);
}
