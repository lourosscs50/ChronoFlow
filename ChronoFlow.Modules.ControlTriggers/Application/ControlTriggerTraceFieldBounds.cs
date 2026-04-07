namespace ChronoFlow.Modules.ControlTriggers.Application;

/// <summary>Bounded operator-safe values for durable execution trace fields (start-time copies only).</summary>
public static class ControlTriggerTraceFieldBounds
{
    public const int MaxCorrelationIdLength = 200;
    public const int MaxOperatorReviewNoteLength = 500;

    /// <summary>Trim and truncate intake correlation for persistence; returns null when absent.</summary>
    public static string? BoundedCorrelationId(string? correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
            return null;
        var t = correlationId.Trim();
        return t.Length <= MaxCorrelationIdLength ? t : t[..MaxCorrelationIdLength];
    }

    /// <summary>Optional operator note for review actions; null when absent.</summary>
    public static string? BoundedOperatorReviewNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
            return null;
        var t = note.Trim();
        return t.Length <= MaxOperatorReviewNoteLength ? t : t[..MaxOperatorReviewNoteLength];
    }
}
