namespace ChronoFlow.Modules.ControlTriggers.Application;

/// <summary>
/// Decides whether workflow execution may proceed for a validated trigger.
/// Keys are recorded only after successful execution via <see cref="RecordSuccessfulExecution"/>.
/// </summary>
public interface IControlTriggerDeduplicator
{
    /// <summary>Called after validation, before routing. Duplicate = same AlertId + LifecycleEventType already executed.</summary>
    ControlTriggerDeduplicationResult EvaluateBeforeRouting(ReceiveControlTriggerCommand command);

    /// <summary>Record suppression key only after workflow has run successfully.</summary>
    void RecordSuccessfulExecution(ReceiveControlTriggerCommand command);
}
