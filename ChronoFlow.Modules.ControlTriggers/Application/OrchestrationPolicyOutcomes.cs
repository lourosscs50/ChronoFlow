namespace ChronoFlow.Modules.ControlTriggers.Application;

/// <summary>Persisted orchestration policy gate outcomes (operator-safe, stable vocabulary).</summary>
public static class OrchestrationPolicyOutcomes
{
    public const string Proceed = "proceed";
    public const string PolicySuppressed = "policy_suppressed";
    public const string AdvisoryOnly = "advisory_only";
    public const string PendingReview = "pending_review";

    /// <summary>Human operator cancelled a pending-review gate; workflow was not executed under this record.</summary>
    public const string ReviewCancelled = "review_cancelled";
}
