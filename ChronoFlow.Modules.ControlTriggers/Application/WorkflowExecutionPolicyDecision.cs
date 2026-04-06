namespace ChronoFlow.Modules.ControlTriggers.Application;

/// <summary>Deterministic outcome of orchestration policy evaluation.</summary>
public sealed record WorkflowExecutionPolicyDecision(WorkflowPolicyKind Kind);
