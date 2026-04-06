namespace ChronoFlow.Modules.ControlTriggers.Application;

/// <summary>
/// Start-time snapshot of advisory and optional intake decision context persisted on execution records (operator-safe, bounded).
/// </summary>
public sealed record AdvisoryExecutionSnapshot(
    bool AdvisoryWasUsed,
    string? AdvisoryStrategyKey,
    string? AdvisoryConfidence,
    string? AdvisoryReasonSummary,
    /// <summary>Honest A.I.L. execution/decision id when returned by dependency; null when not applicable.</summary>
    string? LinkedAilExecutionId,
    string? InboundDecisionSummary,
    string? InboundDecisionReferenceId,
    string? InboundDecisionConfidence,
    string? InboundDecisionReasonCode,
    string? InboundLinkedExternalExecutionId)
{
    public static AdvisoryExecutionSnapshot Empty { get; } = new(
        false,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null);
}
