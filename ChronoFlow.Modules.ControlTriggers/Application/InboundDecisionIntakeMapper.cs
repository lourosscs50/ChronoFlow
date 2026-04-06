namespace ChronoFlow.Modules.ControlTriggers.Application;

/// <summary>Maps optional intake decision context to bounded, operator-safe persisted strings (start-time copy).</summary>
public static class InboundDecisionIntakeMapper
{
    public const int MaxSummaryLength = 500;
    public const int MaxReferenceIdLength = 200;
    public const int MaxConfidenceLength = 50;
    public const int MaxReasonCodeLength = 100;
    public const int MaxLinkedExternalExecutionIdLength = 200;

    public static BoundedInboundDecisionSnapshot Map(InboundDecisionContext? context)
    {
        if (context is null)
            return BoundedInboundDecisionSnapshot.Empty;

        return new BoundedInboundDecisionSnapshot(
            Truncate(context.Summary, MaxSummaryLength),
            Truncate(context.ReferenceId, MaxReferenceIdLength),
            Truncate(context.Confidence, MaxConfidenceLength),
            Truncate(context.ReasonCode, MaxReasonCodeLength),
            Truncate(context.LinkedExternalExecutionId, MaxLinkedExternalExecutionIdLength));
    }

    private static string? Truncate(string? text, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;
        var t = text.Trim();
        return t.Length <= maxLen ? t : t[..maxLen];
    }
}

/// <summary>Bounded copy of intake decision fields at orchestration start (immutable snapshot values).</summary>
public sealed record BoundedInboundDecisionSnapshot(
    string? Summary,
    string? ReferenceId,
    string? Confidence,
    string? ReasonCode,
    string? LinkedExternalExecutionId)
{
    public static BoundedInboundDecisionSnapshot Empty { get; } = new(null, null, null, null, null);
}
