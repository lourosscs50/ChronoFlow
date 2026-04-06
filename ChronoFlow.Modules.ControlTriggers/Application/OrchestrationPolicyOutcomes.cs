namespace ChronoFlow.Modules.ControlTriggers.Application;

/// <summary>Persisted orchestration policy gate outcomes (operator-safe, stable vocabulary).</summary>
public static class OrchestrationPolicyOutcomes
{
    public const string Proceed = "proceed";
    public const string PolicySuppressed = "policy_suppressed";
    public const string AdvisoryOnly = "advisory_only";
    public const string PendingReview = "pending_review";
}
