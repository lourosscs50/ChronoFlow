using ChronoFlow.Modules.ControlTriggers.Domain;

namespace ChronoFlow.Modules.ControlTriggers.Application;

internal static class PendingOperatorReviewRecordMutations
{
    public static ControlExecutionRecord ToApprovedExecuted(
        ControlExecutionRecord o,
        int executedStepCount,
        DateTimeOffset executedAtUtc,
        DateTimeOffset reviewActionAtUtc,
        string? boundedNote) =>
        new()
        {
            Id = o.Id,
            ExecutionInstanceId = o.ExecutionInstanceId,
            TriggerType = o.TriggerType,
            LifecycleEventType = o.LifecycleEventType,
            AlertId = o.AlertId,
            RuleId = o.RuleId,
            SignalId = o.SignalId,
            CorrelationId = o.CorrelationId,
            OccurredAtUtc = o.OccurredAtUtc,
            WorkflowKey = o.WorkflowKey,
            WasExecuted = true,
            WasSuppressed = o.WasSuppressed,
            SuppressionReason = o.SuppressionReason,
            ExecutedStepCount = executedStepCount,
            ReceivedAtUtc = o.ReceivedAtUtc,
            ExecutedAtUtc = executedAtUtc,
            CurrentStatus = o.CurrentStatus,
            AcknowledgedByUserId = o.AcknowledgedByUserId,
            ResolvedByUserId = o.ResolvedByUserId,
            ReopenedByUserId = o.ReopenedByUserId,
            RuleName = o.RuleName,
            HasBeenReopened = o.HasBeenReopened,
            AdvisoryWasUsed = o.AdvisoryWasUsed,
            AdvisoryStrategyKey = o.AdvisoryStrategyKey,
            AdvisoryConfidence = o.AdvisoryConfidence,
            AdvisoryReasonSummary = o.AdvisoryReasonSummary,
            LinkedAilExecutionId = o.LinkedAilExecutionId,
            InboundDecisionSummary = o.InboundDecisionSummary,
            InboundDecisionReferenceId = o.InboundDecisionReferenceId,
            InboundDecisionConfidence = o.InboundDecisionConfidence,
            InboundDecisionReasonCode = o.InboundDecisionReasonCode,
            InboundLinkedExternalExecutionId = o.InboundLinkedExternalExecutionId,
            PendingOperatorReview = false,
            OrchestrationPolicyOutcome = OrchestrationPolicyOutcomes.Proceed,
            OperatorReviewAction = OperatorReviewActions.Approved,
            OperatorReviewActionAtUtc = reviewActionAtUtc,
            OperatorReviewNote = boundedNote
        };

    public static ControlExecutionRecord ToCancelled(
        ControlExecutionRecord o,
        DateTimeOffset reviewActionAtUtc,
        string? boundedNote) =>
        new()
        {
            Id = o.Id,
            ExecutionInstanceId = o.ExecutionInstanceId,
            TriggerType = o.TriggerType,
            LifecycleEventType = o.LifecycleEventType,
            AlertId = o.AlertId,
            RuleId = o.RuleId,
            SignalId = o.SignalId,
            CorrelationId = o.CorrelationId,
            OccurredAtUtc = o.OccurredAtUtc,
            WorkflowKey = o.WorkflowKey,
            WasExecuted = false,
            WasSuppressed = o.WasSuppressed,
            SuppressionReason = o.SuppressionReason,
            ExecutedStepCount = 0,
            ReceivedAtUtc = o.ReceivedAtUtc,
            ExecutedAtUtc = null,
            CurrentStatus = o.CurrentStatus,
            AcknowledgedByUserId = o.AcknowledgedByUserId,
            ResolvedByUserId = o.ResolvedByUserId,
            ReopenedByUserId = o.ReopenedByUserId,
            RuleName = o.RuleName,
            HasBeenReopened = o.HasBeenReopened,
            AdvisoryWasUsed = o.AdvisoryWasUsed,
            AdvisoryStrategyKey = o.AdvisoryStrategyKey,
            AdvisoryConfidence = o.AdvisoryConfidence,
            AdvisoryReasonSummary = o.AdvisoryReasonSummary,
            LinkedAilExecutionId = o.LinkedAilExecutionId,
            InboundDecisionSummary = o.InboundDecisionSummary,
            InboundDecisionReferenceId = o.InboundDecisionReferenceId,
            InboundDecisionConfidence = o.InboundDecisionConfidence,
            InboundDecisionReasonCode = o.InboundDecisionReasonCode,
            InboundLinkedExternalExecutionId = o.InboundLinkedExternalExecutionId,
            PendingOperatorReview = false,
            OrchestrationPolicyOutcome = OrchestrationPolicyOutcomes.ReviewCancelled,
            OperatorReviewAction = OperatorReviewActions.Cancelled,
            OperatorReviewActionAtUtc = reviewActionAtUtc,
            OperatorReviewNote = boundedNote
        };
}
