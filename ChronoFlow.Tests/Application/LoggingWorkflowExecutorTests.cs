using ChronoFlow.Modules.ControlTriggers.Application;
using ChronoFlow.Modules.ControlTriggers.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ChronoFlow.Tests.Application;

public sealed class LoggingWorkflowExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_runs_steps_sorted_by_order()
    {
        var sut = new LoggingWorkflowExecutor(NullLogger<LoggingWorkflowExecutor>.Instance);
        var definition = new WorkflowDefinition(
            "test-workflow",
            null,
            [
                new WorkflowStepDefinition("Third", "Internal", 3),
                new WorkflowStepDefinition("First", "Internal", 1),
                new WorkflowStepDefinition("Second", "Internal", 2)
            ]);
        var trigger = new ReceiveControlTriggerCommand(
            "AlertCreated",
            Guid.Parse("a1000000-0000-0000-0000-000000000001"),
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

        var result = await sut.ExecuteAsync(definition, trigger, CancellationToken.None);

        Assert.Equal(3, result.ExecutedStepCount);
    }
}
