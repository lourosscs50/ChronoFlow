namespace ChronoFlow.Modules.ControlTriggers.Application;

/// <summary>
/// Well-known <see cref="InboundDecisionContext.ReasonCode"/> values interpreted by the default orchestration policy.
/// Upstream intake may set these to trigger deterministic platform behavior (not tenant business rules).
/// </summary>
public static class OrchestrationPolicyInboundReasonCodes
{
    public const string SuppressExecution = "CHRONOFLOW_ORCH_SUPPRESS";
    public const string AdvisoryOnly = "CHRONOFLOW_ORCH_ADVISORY_ONLY";
    public const string RequireReview = "CHRONOFLOW_ORCH_REQUIRE_REVIEW";
}
