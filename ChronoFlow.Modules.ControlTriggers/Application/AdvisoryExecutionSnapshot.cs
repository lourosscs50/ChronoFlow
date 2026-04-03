namespace ChronoFlow.Modules.ControlTriggers.Application;

/// <summary>Minimal fields persisted on execution records for operator inspection (no large payloads).</summary>
public sealed record AdvisoryExecutionSnapshot(
    bool AdvisoryWasUsed,
    string? AdvisoryStrategyKey,
    string? AdvisoryConfidence,
    string? AdvisoryReasonSummary);
