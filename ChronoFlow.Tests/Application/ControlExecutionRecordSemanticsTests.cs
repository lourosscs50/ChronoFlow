using ChronoFlow.Modules.ControlTriggers.Application;
using ChronoFlow.Modules.ControlTriggers.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ChronoFlow.Tests.Application;

/// <summary>Phase 6.7–6.8: locked semantics for executed vs suppressed vs accepted-no-workflow.</summary>
public sealed class ControlExecutionRecordSemanticsTests
{
    [Fact]
    public async Task Executed_record_matches_locked_semantics()
    {
        var repo = new InMemoryControlExecutionRecordRepository();
        var sut = new ReceiveControlTriggerHandler(
            NullLogger<ReceiveControlTriggerHandler>.Instance,
            ControlTriggerHandlerTestDefaults.DisabledAdvisoryOptions,
            new DefaultControlTriggerDeduplicator(new InMemoryProcessedTriggerStore()),
            new DefaultControlTriggerRouter(),
            new NoOpControlDecisionAdvisor(),
            new DefaultWorkflowExecutionPolicy(),
            new RecordingExecutor(),
            repo);

        var cmd = BaseCommand("AlertCreated", "AlertCreated");
        var result = await sut.HandleAsync(cmd, CancellationToken.None);

        Assert.True(result.Accepted);
        Assert.True(result.WasExecuted);
        Assert.False(result.WasSuppressed);
        Assert.Equal(result.ExecutionRecordId, repo.Records.Single().Id);
        Assert.NotNull(result.ExecutionInstanceId);
        Assert.NotEqual(Guid.Empty, result.ExecutionInstanceId);

        var r = repo.Records.Single();
        Assert.Equal(result.ExecutionInstanceId, r.ExecutionInstanceId);
        Assert.NotEqual(r.Id, r.ExecutionInstanceId);
        Assert.True(r.WasExecuted);
        Assert.False(r.WasSuppressed);
        Assert.Null(r.SuppressionReason);
        Assert.NotNull(r.WorkflowKey);
        Assert.True(r.ExecutedStepCount > 0);
        Assert.NotNull(r.ExecutedAtUtc);
        Assert.Equal("alert-created-default", r.WorkflowKey);
        Assert.False(r.AdvisoryWasUsed);
        Assert.False(r.PendingOperatorReview);
        Assert.Equal(OrchestrationPolicyOutcomes.Proceed, r.OrchestrationPolicyOutcome);
        Assert.Equal(cmd.OccurredAtUtc, r.OccurredAtUtc);
    }

    [Fact]
    public async Task Suppressed_record_matches_locked_semantics()
    {
        var store = new InMemoryProcessedTriggerStore();
        var repo = new InMemoryControlExecutionRecordRepository();
        var sut = new ReceiveControlTriggerHandler(
            NullLogger<ReceiveControlTriggerHandler>.Instance,
            ControlTriggerHandlerTestDefaults.DisabledAdvisoryOptions,
            new DefaultControlTriggerDeduplicator(store),
            new DefaultControlTriggerRouter(),
            new NoOpControlDecisionAdvisor(),
            new DefaultWorkflowExecutionPolicy(),
            new CountingExecutor(),
            repo);

        var cmd = BaseCommand("AlertCreated", "AlertCreated");
        await sut.HandleAsync(cmd, CancellationToken.None);
        var second = await sut.HandleAsync(cmd, CancellationToken.None);

        Assert.True(second.WasSuppressed);
        Assert.False(second.WasExecuted);
        var suppressed = repo.Records.Last();
        Assert.False(suppressed.WasExecuted);
        Assert.True(suppressed.WasSuppressed);
        Assert.NotNull(suppressed.SuppressionReason);
        Assert.Null(suppressed.WorkflowKey);
        Assert.Equal(0, suppressed.ExecutedStepCount);
        Assert.Null(suppressed.ExecutedAtUtc);
        Assert.Null(suppressed.ExecutionInstanceId);
    }

    [Fact]
    public async Task Accepted_no_workflow_record_matches_locked_semantics()
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

        var cmd = BaseCommand("AlertAcknowledged", "AlertAcknowledged");
        var result = await sut.HandleAsync(cmd, CancellationToken.None);

        Assert.True(result.Accepted);
        Assert.False(result.WasExecuted);
        Assert.False(result.WasSuppressed);
        Assert.Null(result.ExecutionInstanceId);

        var r = repo.Records.Single();
        Assert.False(r.WasExecuted);
        Assert.False(r.WasSuppressed);
        Assert.Null(r.WorkflowKey);
        Assert.Equal(0, r.ExecutedStepCount);
        Assert.Null(r.ExecutedAtUtc);
        Assert.Null(r.ExecutionInstanceId);
    }

    [Fact]
    public async Task Invalid_request_does_not_persist_record()
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

        var cmd = BaseCommand("AlertCreated", "AlertCreated") with { AlertId = Guid.Empty };
        var result = await sut.HandleAsync(cmd, CancellationToken.None);

        Assert.False(result.Accepted);
        Assert.Empty(repo.Records);
    }

    private static ReceiveControlTriggerCommand BaseCommand(string triggerType, string lifecycle) =>
        new(
            triggerType,
            Guid.Parse("a1000000-0000-0000-0000-000000000001"),
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

    private sealed class RecordingExecutor : IWorkflowExecutor
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
}
