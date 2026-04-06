using ChronoFlow.Modules.ControlTriggers.Domain;

namespace ChronoFlow.Modules.ControlTriggers.Application;

public enum PendingReviewActionFailureKind
{
    NotFound,
    NotPendingReview,
    InvalidRecordState,
    WorkflowResolutionMismatch
}

/// <summary>Outcome of approve/cancel on a pending operator review execution record.</summary>
public readonly struct PendingReviewActionResult
{
    public bool Succeeded { get; }
    public PendingReviewActionFailureKind? Failure { get; }
    public ControlExecutionRecord? Record { get; }

    private PendingReviewActionResult(bool succeeded, PendingReviewActionFailureKind? failure, ControlExecutionRecord? record)
    {
        Succeeded = succeeded;
        Failure = failure;
        Record = record;
    }

    public static PendingReviewActionResult Ok(ControlExecutionRecord record) => new(true, null, record);

    public static PendingReviewActionResult Fail(PendingReviewActionFailureKind failure) => new(false, failure, null);
}
