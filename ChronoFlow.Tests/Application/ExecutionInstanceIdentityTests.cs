using ChronoFlow.Modules.ControlTriggers.Application;
using ChronoFlow.Modules.ControlTriggers.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ChronoFlow.Tests.Application;

/// <summary>Phase 1: execution instance identity is created at workflow start, persisted, and distinct from the durable record id.</summary>
public sealed class ExecutionInstanceIdentityTests
{
    [Fact]
    public async Task Two_sequential_executions_produce_distinct_execution_instance_ids()
    {
        var repo = new InMemoryControlExecutionRecordRepository();
        var sut = new ReceiveControlTriggerHandler(
            NullLogger<ReceiveControlTriggerHandler>.Instance,
            ControlTriggerHandlerTestDefaults.DisabledAdvisoryOptions,
            new DefaultControlTriggerDeduplicator(new InMemoryProcessedTriggerStore()),
            new DefaultControlTriggerRouter(),
            new NoOpControlDecisionAdvisor(),
            new DefaultWorkflowExecutionPolicy(),
            new CountingExecutor(),
            repo);

        var cmd1 = Command(Guid.Parse("a1000000-0000-4000-8000-000000000001"));
        var cmd2 = Command(Guid.Parse("a2000000-0000-4000-8000-000000000002"));

        var first = await sut.HandleAsync(cmd1, CancellationToken.None);
        var second = await sut.HandleAsync(cmd2, CancellationToken.None);

        Assert.True(first.WasExecuted);
        Assert.True(second.WasExecuted);
        Assert.NotNull(first.ExecutionInstanceId);
        Assert.NotNull(second.ExecutionInstanceId);
        Assert.NotEqual(first.ExecutionInstanceId, second.ExecutionInstanceId);
    }

    [Fact]
    public void ReceiveControlTriggerCommand_exposes_optional_correlation_id_for_intake_propagation()
    {
        var t = typeof(ReceiveControlTriggerCommand);
        var correlation = t.GetProperty("CorrelationId");
        Assert.NotNull(correlation);
        Assert.Equal(typeof(string), correlation.PropertyType);
    }

    private sealed class CountingExecutor : IWorkflowExecutor
    {
        public Task<WorkflowExecutionResult> ExecuteAsync(
            Guid executionInstanceId,
            WorkflowDefinition definition,
            ReceiveControlTriggerCommand trigger,
            CancellationToken cancellationToken = default)
        {
            _ = trigger;
            _ = cancellationToken;
            return Task.FromResult(new WorkflowExecutionResult(definition.Steps.Count, executionInstanceId));
        }
    }

    private static ReceiveControlTriggerCommand Command(Guid alertId) =>
        new(
            "AlertCreated",
            alertId,
            Guid.Parse("b2000000-0000-4000-8000-000000000002"),
            Guid.Parse("c3000000-0000-4000-8000-000000000003"),
            new DateTimeOffset(2026, 4, 5, 12, 0, 0, TimeSpan.Zero),
            "Open",
            "AlertCreated",
            null,
            null,
            null,
            "Rule",
            false);
}
