namespace ChronoFlow.Modules.ControlTriggers.Application;

public sealed record ControlExecutionRecordQuery(
    Guid? AlertId = null,
    string? LifecycleEventType = null,
    bool? WasExecuted = null,
    bool? WasSuppressed = null,
    string? WorkflowKey = null,
    int Skip = 0,
    int Take = 50);
