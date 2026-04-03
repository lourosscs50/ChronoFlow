using ChronoFlow.Modules.ControlTriggers.Application;
using Xunit;

namespace ChronoFlow.Tests.Application;

public sealed class DefaultControlTriggerRouterTests
{
    private readonly DefaultControlTriggerRouter _router = new();

    [Theory]
    [InlineData("AlertCreated", "alert-created-default")]
    [InlineData("AlertReopened", "alert-reopened-default")]
    public void ResolveWorkflow_maps_lifecycle_to_expected_key(string lifecycle, string expectedKey)
    {
        var command = new ReceiveControlTriggerCommand(
            lifecycle,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "Open",
            lifecycle,
            null,
            null,
            null,
            null,
            false);

        var def = _router.ResolveWorkflow(command);

        Assert.NotNull(def);
        Assert.Equal(expectedKey, def!.WorkflowKey);
    }

    [Theory]
    [InlineData("AlertAcknowledged")]
    [InlineData("AlertResolved")]
    [InlineData("Unknown")]
    public void ResolveWorkflow_returns_null_for_non_routed_lifecycles(string lifecycle)
    {
        var command = new ReceiveControlTriggerCommand(
            lifecycle,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "Open",
            lifecycle,
            null,
            null,
            null,
            null,
            false);

        Assert.Null(_router.ResolveWorkflow(command));
    }

    [Fact]
    public void ResolveWorkflow_ContextEscalated_hint_selects_escalated_created_workflow()
    {
        var command = new ReceiveControlTriggerCommand(
            "AlertCreated",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "Open",
            "AlertCreated",
            null,
            null,
            null,
            null,
            false);

        var def = _router.ResolveWorkflow(
            command,
            new ControlAdvisoryRouteHint(AdvisoryStrategyKeys.ContextEscalated));

        Assert.NotNull(def);
        Assert.Equal("alert-created-escalated", def!.WorkflowKey);
    }

    [Fact]
    public void ResolveWorkflow_Unknown_strategy_falls_back_to_default_for_lifecycle()
    {
        var command = new ReceiveControlTriggerCommand(
            "AlertCreated",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "Open",
            "AlertCreated",
            null,
            null,
            null,
            null,
            false);

        var def = _router.ResolveWorkflow(
            command,
            new ControlAdvisoryRouteHint("totally_unknown_strategy_key"));

        Assert.NotNull(def);
        Assert.Equal("alert-created-default", def!.WorkflowKey);
    }
}
