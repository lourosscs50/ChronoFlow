using ChronoFlow.Modules.ControlTriggers.Application;
using Xunit;

namespace ChronoFlow.Tests.Application;

public sealed class ControlTriggerTraceFieldBoundsTests
{
    [Fact]
    public void BoundedCorrelationId_truncates_long_values_deterministically()
    {
        var longVal = new string('c', ControlTriggerTraceFieldBounds.MaxCorrelationIdLength + 50);
        var bounded = ControlTriggerTraceFieldBounds.BoundedCorrelationId(longVal);
        Assert.Equal(ControlTriggerTraceFieldBounds.MaxCorrelationIdLength, bounded!.Length);
        Assert.All(bounded, ch => Assert.Equal('c', ch));
    }

    [Fact]
    public void BoundedCorrelationId_returns_null_for_whitespace()
    {
        Assert.Null(ControlTriggerTraceFieldBounds.BoundedCorrelationId("   "));
        Assert.Null(ControlTriggerTraceFieldBounds.BoundedCorrelationId(null));
    }

    [Fact]
    public void BoundedOperatorReviewNote_truncates_and_trims()
    {
        var longNote = new string('n', ControlTriggerTraceFieldBounds.MaxOperatorReviewNoteLength + 10);
        var b = ControlTriggerTraceFieldBounds.BoundedOperatorReviewNote(longNote);
        Assert.Equal(ControlTriggerTraceFieldBounds.MaxOperatorReviewNoteLength, b!.Length);
        Assert.Null(ControlTriggerTraceFieldBounds.BoundedOperatorReviewNote("  "));
    }
}
