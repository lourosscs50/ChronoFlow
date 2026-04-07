namespace ChronoFlow.Modules.ControlTriggers.Application;

/// <summary>Summary of a single synchronous-style workflow run (no durable history).</summary>
public sealed record WorkflowExecutionResult(int ExecutedStepCount, Guid ExecutionInstanceId);
