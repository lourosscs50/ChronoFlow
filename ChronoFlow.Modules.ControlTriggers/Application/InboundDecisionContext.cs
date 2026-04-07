namespace ChronoFlow.Modules.ControlTriggers.Application;

/// <summary>
/// Optional, bounded decision/advisory context supplied at intake (upstream structured output).
/// ChronoFlow does not interpret or compute this; it is snapshotted honestly at orchestration start.
/// </summary>
public sealed record InboundDecisionContext(
    string? Summary,
    string? ReferenceId,
    string? Confidence,
    string? ReasonCode,
    string? LinkedExternalExecutionId);
