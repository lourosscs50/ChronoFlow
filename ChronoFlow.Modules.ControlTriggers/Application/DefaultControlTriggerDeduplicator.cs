namespace ChronoFlow.Modules.ControlTriggers.Application;

/// <summary>Suppresses repeat execution for the same alert + lifecycle event (in-memory store only).</summary>
public sealed class DefaultControlTriggerDeduplicator(IProcessedTriggerStore store) : IControlTriggerDeduplicator
{
    private const string DuplicateReason =
        "Duplicate trigger: workflow already executed for this alert and lifecycle event.";

    public ControlTriggerDeduplicationResult EvaluateBeforeRouting(ReceiveControlTriggerCommand command)
    {
        var key = ToKey(command);
        if (store.Contains(key))
            return ControlTriggerDeduplicationResult.Duplicate(DuplicateReason);
        return ControlTriggerDeduplicationResult.Proceed();
    }

    public void RecordSuccessfulExecution(ReceiveControlTriggerCommand command)
    {
        store.Add(ToKey(command));
    }

    private static ControlTriggerSuppressionKey ToKey(ReceiveControlTriggerCommand command) =>
        new(command.AlertId, command.LifecycleEventType);
}
