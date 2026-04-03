namespace ChronoFlow.Modules.ControlTriggers.Domain;

/// <summary>In-memory workflow definition keyed for control-trigger routing (ChronoFlow-local).</summary>
public sealed record WorkflowDefinition(
    string WorkflowKey,
    string? Description,
    IReadOnlyList<WorkflowStepDefinition> Steps);
