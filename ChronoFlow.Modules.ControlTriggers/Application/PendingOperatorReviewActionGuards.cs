using ChronoFlow.Modules.ControlTriggers.Domain;

namespace ChronoFlow.Modules.ControlTriggers.Application;

/// <summary>Shared eligibility checks for Phase 5 review actions (Phase 6: explicit finalized vs not-pending).</summary>
internal static class PendingOperatorReviewActionGuards
{
    /// <summary>When non-null, the handler must fail without mutating orchestration state.</summary>
    public static PendingReviewActionFailureKind? ClassifyBlocking(ControlExecutionRecord record)
    {
        if (!string.IsNullOrWhiteSpace(record.OperatorReviewAction))
            return PendingReviewActionFailureKind.AlreadyFinalized;

        if (!IsEligiblePendingReview(record))
            return PendingReviewActionFailureKind.NotPendingReview;

        return null;
    }

    public static bool IsEligiblePendingReview(ControlExecutionRecord record) =>
        record.PendingOperatorReview
        && string.Equals(record.OrchestrationPolicyOutcome, OrchestrationPolicyOutcomes.PendingReview, StringComparison.Ordinal)
        && !record.WasExecuted
        && record.OperatorReviewAction is null;
}
