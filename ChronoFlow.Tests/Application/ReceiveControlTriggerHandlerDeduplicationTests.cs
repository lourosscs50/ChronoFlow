using ChronoFlow.Modules.ControlTriggers.Application;
using ChronoFlow.Modules.ControlTriggers.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ChronoFlow.Tests.Application;

/// <summary>
/// Phase 6.6: suppression key = (AlertId, LifecycleEventType). Store records only after successful workflow execution.
/// </summary>
public sealed class ReceiveControlTriggerHandlerDeduplicationTests
{
    private static ReceiveControlTriggerHandler CreateSut(
        IProcessedTriggerStore store,
        IWorkflowExecutor executor,
        IControlExecutionRecordRepository? executionRecords = null) =>
        new(
            NullLogger<ReceiveControlTriggerHandler>.Instance,
            ControlTriggerHandlerTestDefaults.DisabledAdvisoryOptions,
            new DefaultControlTriggerDeduplicator(store),
            new DefaultControlTriggerRouter(),
            new NoOpControlDecisionAdvisor(),
            executor,
            executionRecords ?? new InMemoryControlExecutionRecordRepository());

    [Fact]
    public async Task First_AlertCreated_executes_second_identical_command_suppressed()
    {
        var store = new InMemoryProcessedTriggerStore();
        var counter = new CountingWorkflowExecutor();
        var repo = new InMemoryControlExecutionRecordRepository();
        var sut = CreateSut(store, counter, repo);
        var command = Command("AlertCreated", "AlertCreated");

        var first = await sut.HandleAsync(command, CancellationToken.None);
        var second = await sut.HandleAsync(command, CancellationToken.None);

        Assert.True(first.WasExecuted);
        Assert.False(first.WasSuppressed);
        Assert.Equal(1, counter.ExecuteCallCount);

        Assert.False(second.WasExecuted);
        Assert.True(second.WasSuppressed);
        Assert.NotNull(second.SuppressionReason);
        Assert.Equal(1, counter.ExecuteCallCount);

        Assert.Equal(2, repo.Records.Count);
        Assert.True(repo.Records[0].WasExecuted);
        Assert.False(repo.Records[0].WasSuppressed);
        Assert.False(repo.Records[1].WasExecuted);
        Assert.True(repo.Records[1].WasSuppressed);
        Assert.NotNull(repo.Records[1].SuppressionReason);
    }

    [Fact]
    public async Task AlertReopened_executes_after_AlertCreated_on_same_alert_both_distinct_keys()
    {
        var store = new InMemoryProcessedTriggerStore();
        var counter = new CountingWorkflowExecutor();
        var sut = CreateSut(store, counter);
        var alertId = Guid.Parse("e1000000-0000-0000-0000-000000000001");

        var created = await sut.HandleAsync(CommandForAlert(alertId, "AlertCreated", "AlertCreated"), CancellationToken.None);
        var reopened = await sut.HandleAsync(CommandForAlert(alertId, "AlertReopened", "AlertReopened"), CancellationToken.None);

        Assert.True(created.WasExecuted);
        Assert.True(reopened.WasExecuted);
        Assert.False(reopened.WasSuppressed);
        Assert.Equal(2, counter.ExecuteCallCount);
    }

    [Fact]
    public async Task Second_identical_AlertReopened_suppressed()
    {
        var store = new InMemoryProcessedTriggerStore();
        var counter = new CountingWorkflowExecutor();
        var sut = CreateSut(store, counter);
        var cmd = Command("AlertReopened", "AlertReopened");

        await sut.HandleAsync(cmd, CancellationToken.None);
        var second = await sut.HandleAsync(cmd, CancellationToken.None);

        Assert.True(second.WasSuppressed);
        Assert.Equal(1, counter.ExecuteCallCount);
    }

    [Fact]
    public async Task Different_alert_ids_do_not_suppress_each_other()
    {
        var store = new InMemoryProcessedTriggerStore();
        var counter = new CountingWorkflowExecutor();
        var sut = CreateSut(store, counter);
        var a = CommandForAlert(Guid.Parse("a1000000-0000-0000-0000-000000000001"), "AlertCreated", "AlertCreated");
        var b = CommandForAlert(Guid.Parse("b2000000-0000-0000-0000-000000000002"), "AlertCreated", "AlertCreated");

        var r1 = await sut.HandleAsync(a, CancellationToken.None);
        var r2 = await sut.HandleAsync(b, CancellationToken.None);

        Assert.True(r1.WasExecuted);
        Assert.True(r2.WasExecuted);
        Assert.False(r2.WasSuppressed);
        Assert.Equal(2, counter.ExecuteCallCount);
    }

    [Fact]
    public async Task Seeded_store_suppresses_without_calling_executor()
    {
        var store = new InMemoryProcessedTriggerStore();
        var alertId = Guid.Parse("c3000000-0000-0000-0000-000000000003");
        store.Add(new ControlTriggerSuppressionKey(alertId, "AlertCreated"));
        var counter = new CountingWorkflowExecutor();
        var sut = CreateSut(store, counter);

        var result = await sut.HandleAsync(CommandForAlert(alertId, "AlertCreated", "AlertCreated"), CancellationToken.None);

        Assert.True(result.WasSuppressed);
        Assert.False(result.WasExecuted);
        Assert.Equal(0, counter.ExecuteCallCount);
    }

    [Fact]
    public async Task No_workflow_match_is_not_recorded_repeat_AlertAcknowledged_neither_suppressed()
    {
        var store = new InMemoryProcessedTriggerStore();
        var counter = new CountingWorkflowExecutor();
        var sut = CreateSut(store, counter);
        var cmd = Command("AlertAcknowledged", "AlertAcknowledged");

        var first = await sut.HandleAsync(cmd, CancellationToken.None);
        var second = await sut.HandleAsync(cmd, CancellationToken.None);

        Assert.True(first.Accepted);
        Assert.False(first.WasExecuted);
        Assert.False(first.WasSuppressed);
        Assert.True(second.Accepted);
        Assert.False(second.WasExecuted);
        Assert.False(second.WasSuppressed);
        Assert.Equal(0, counter.ExecuteCallCount);
    }

    private static ReceiveControlTriggerCommand Command(string triggerType, string lifecycle) =>
        CommandForAlert(Guid.Parse("d4000000-0000-0000-0000-000000000004"), triggerType, lifecycle);

    private static ReceiveControlTriggerCommand CommandForAlert(Guid alertId, string triggerType, string lifecycle) =>
        new(
            triggerType,
            alertId,
            Guid.Parse("b2000000-0000-0000-0000-000000000002"),
            Guid.Parse("c3000000-0000-0000-0000-000000000003"),
            new DateTimeOffset(2026, 4, 3, 10, 0, 0, TimeSpan.Zero),
            "Open",
            lifecycle,
            null,
            null,
            null,
            "Rule",
            false);

    private sealed class CountingWorkflowExecutor : IWorkflowExecutor
    {
        public int ExecuteCallCount { get; private set; }

        public Task<WorkflowExecutionResult> ExecuteAsync(
            Guid executionInstanceId,
            WorkflowDefinition definition,
            ReceiveControlTriggerCommand trigger,
            CancellationToken cancellationToken = default)
        {
            _ = trigger;
            _ = cancellationToken;
            ExecuteCallCount++;
            return Task.FromResult(new WorkflowExecutionResult(definition.Steps.Count, executionInstanceId));
        }
    }
}
