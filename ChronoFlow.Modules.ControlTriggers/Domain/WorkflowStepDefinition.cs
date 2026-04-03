namespace ChronoFlow.Modules.ControlTriggers.Domain;

/// <summary>Minimal workflow step for deterministic in-process execution (no persistence).</summary>
public sealed record WorkflowStepDefinition(string StepName, string StepType, int Order);
