namespace ChronoFlow.Modules.ControlTriggers.Application;

/// <summary>Normalized advisory outcome from AIL (or a test double), used only for local routing hints.</summary>
public sealed record ControlAdvisoryOutcome(
    string SelectedStrategyKey,
    string Confidence,
    string ReasonSummary,
    bool UsedMemory,
    int MemoryItemCount,
    /// <summary>Returned by A.I.L. when it allocates its own execution/decision id; never invented by ChronoFlow.</summary>
    string? LinkedAilExecutionId = null);
