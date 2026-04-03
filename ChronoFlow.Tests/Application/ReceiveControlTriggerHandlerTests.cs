using ChronoFlow.Modules.ControlTriggers.Application;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ChronoFlow.Tests.Application;

public sealed class ReceiveControlTriggerHandlerTests
{
    private static ReceiveControlTriggerHandler CreateSut(
        IControlTriggerRouter? router = null,
        IWorkflowExecutor? executor = null,
        IControlTriggerDeduplicator? deduplicator = null,
        IControlExecutionRecordRepository? executionRecords = null) =>
        new(
            NullLogger<ReceiveControlTriggerHandler>.Instance,
            ControlTriggerHandlerTestDefaults.DisabledAdvisoryOptions,
            deduplicator ?? new DefaultControlTriggerDeduplicator(new InMemoryProcessedTriggerStore()),
            router ?? new DefaultControlTriggerRouter(),
            new NoOpControlDecisionAdvisor(),
            executor ?? new LoggingWorkflowExecutor(NullLogger<LoggingWorkflowExecutor>.Instance),
            executionRecords ?? new InMemoryControlExecutionRecordRepository());

    [Fact]
    public async Task HandleAsync_accepts_normalized_command_and_executes_for_AlertReopened()
    {
        var sut = CreateSut();
        var command = new ReceiveControlTriggerCommand(
            "AlertReopened",
            Guid.Parse("a1000000-0000-0000-0000-000000000001"),
            Guid.Parse("b2000000-0000-0000-0000-000000000002"),
            Guid.Parse("c3000000-0000-0000-0000-000000000003"),
            new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero),
            "Open",
            "AlertReopened",
            "u1",
            null,
            "u2",
            "R",
            true);

        var result = await sut.HandleAsync(command, CancellationToken.None);

        Assert.True(result.Accepted);
        Assert.Null(result.ErrorMessage);
        Assert.True(result.WasExecuted);
        Assert.False(result.WasSuppressed);
        Assert.Equal("alert-reopened-default", result.WorkflowKey);
        Assert.Equal(3, result.ExecutedStepCount);
        Assert.NotNull(result.ExecutionRecordId);
    }

    [Fact]
    public async Task HandleAsync_rejects_empty_alert_id()
    {
        var sut = CreateSut();
        var command = new ReceiveControlTriggerCommand(
            "AlertCreated",
            Guid.Empty,
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

        var result = await sut.HandleAsync(command, CancellationToken.None);

        Assert.False(result.Accepted);
        Assert.NotNull(result.ErrorMessage);
        Assert.False(result.WasExecuted);
        Assert.False(result.WasSuppressed);
        Assert.Null(result.ExecutionRecordId);
    }
}
