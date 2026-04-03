namespace ChronoFlow.Modules.ControlTriggers.Application;

public sealed record ReceiveControlTriggerResult(
    bool Accepted,
    string? ErrorMessage = null,
    bool WasExecuted = false,
    bool WasSuppressed = false,
    string? SuppressionReason = null,
    string? WorkflowKey = null,
    int ExecutedStepCount = 0,
    Guid? ExecutionRecordId = null)
{
    public static ReceiveControlTriggerResult OkNotExecuted(Guid executionRecordId) =>
        new(true, null, false, false, null, null, 0, executionRecordId);

    public static ReceiveControlTriggerResult OkSuppressed(string suppressionReason, Guid executionRecordId) =>
        new(true, null, false, true, suppressionReason, null, 0, executionRecordId);

    public static ReceiveControlTriggerResult OkExecuted(
        string workflowKey,
        int executedStepCount,
        Guid executionRecordId) =>
        new(true, null, true, false, null, workflowKey, executedStepCount, executionRecordId);

    public static ReceiveControlTriggerResult Invalid(string error) =>
        new(false, error, false, false, null, null, 0, null);
}
