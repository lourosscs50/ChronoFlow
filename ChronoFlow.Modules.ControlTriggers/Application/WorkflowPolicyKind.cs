namespace ChronoFlow.Modules.ControlTriggers.Application;

/// <summary>Orchestration-level execution gate decided at trigger time (no external I/O).</summary>
public enum WorkflowPolicyKind
{
    Proceed = 0,
    Suppress = 1,
    AdvisoryOnly = 2,
    RequireReview = 3
}
