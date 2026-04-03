namespace ChronoFlow.Modules.ControlTriggers.Application;

/// <summary>Thread-safe in-process store of successfully executed trigger keys.</summary>
public sealed class InMemoryProcessedTriggerStore : IProcessedTriggerStore
{
    private readonly HashSet<ControlTriggerSuppressionKey> _keys = new();
    private readonly object _sync = new();

    public bool Contains(ControlTriggerSuppressionKey key)
    {
        lock (_sync)
            return _keys.Contains(key);
    }

    public void Add(ControlTriggerSuppressionKey key)
    {
        lock (_sync)
            _keys.Add(key);
    }
}
