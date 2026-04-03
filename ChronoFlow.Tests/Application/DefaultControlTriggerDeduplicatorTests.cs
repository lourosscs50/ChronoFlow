using ChronoFlow.Modules.ControlTriggers.Application;
using Xunit;

namespace ChronoFlow.Tests.Application;

public sealed class DefaultControlTriggerDeduplicatorTests
{
    private readonly InMemoryProcessedTriggerStore _store = new();
    private readonly DefaultControlTriggerDeduplicator _sut;

    public DefaultControlTriggerDeduplicatorTests()
    {
        _sut = new DefaultControlTriggerDeduplicator(_store);
    }

    [Fact]
    public void EvaluateBeforeRouting_proceeds_when_key_absent()
    {
        var cmd = Command(Guid.NewGuid(), "AlertCreated");

        var r = _sut.EvaluateBeforeRouting(cmd);

        Assert.True(r.ShouldExecute);
        Assert.False(r.WasSuppressed);
        Assert.Null(r.SuppressionReason);
    }

    [Fact]
    public void EvaluateBeforeRouting_suppresses_after_RecordSuccessfulExecution()
    {
        var cmd = Command(Guid.NewGuid(), "AlertCreated");

        Assert.True(_sut.EvaluateBeforeRouting(cmd).ShouldExecute);

        _sut.RecordSuccessfulExecution(cmd);

        var second = _sut.EvaluateBeforeRouting(cmd);
        Assert.False(second.ShouldExecute);
        Assert.True(second.WasSuppressed);
        Assert.NotNull(second.SuppressionReason);
    }

    [Fact]
    public void Different_lifecycle_same_alert_are_independent_keys()
    {
        var alertId = Guid.NewGuid();
        var created = Command(alertId, "AlertCreated");
        var reopened = Command(alertId, "AlertReopened");

        _sut.RecordSuccessfulExecution(created);

        Assert.True(_sut.EvaluateBeforeRouting(reopened).ShouldExecute);
    }

    private static ReceiveControlTriggerCommand Command(Guid alertId, string lifecycle) =>
        new(
            lifecycle,
            alertId,
            Guid.Parse("b2000000-0000-0000-0000-000000000002"),
            Guid.Parse("c3000000-0000-0000-0000-000000000003"),
            DateTimeOffset.UtcNow,
            "Open",
            lifecycle,
            null,
            null,
            null,
            null,
            false);
}
