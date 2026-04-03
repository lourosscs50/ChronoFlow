namespace ChronoFlow.Modules.ControlTriggers.Application;

/// <summary>Tracks (AlertId, LifecycleEventType) pairs that have completed workflow execution (no persistence).</summary>
public interface IProcessedTriggerStore
{
    bool Contains(ControlTriggerSuppressionKey key);

    /// <summary>Adds a key after successful workflow execution. Idempotent.</summary>
    void Add(ControlTriggerSuppressionKey key);
}
