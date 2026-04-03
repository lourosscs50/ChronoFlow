namespace ChronoFlow.Modules.ControlTriggers.Application;

/// <summary>Outcome of duplicate detection before routing/execution.</summary>
public sealed record ControlTriggerDeduplicationResult(
    bool ShouldExecute,
    bool WasSuppressed,
    string? SuppressionReason)
{
    public static ControlTriggerDeduplicationResult Proceed() =>
        new(true, false, null);

    public static ControlTriggerDeduplicationResult Duplicate(string reason) =>
        new(false, true, reason);
}
