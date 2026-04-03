namespace ChronoFlow.Modules.ControlTriggers.Application;

/// <summary>Suppresses repeat workflow execution for the same alert and lifecycle event (in-memory only).</summary>
public readonly record struct ControlTriggerSuppressionKey(Guid AlertId, string LifecycleEventType);
