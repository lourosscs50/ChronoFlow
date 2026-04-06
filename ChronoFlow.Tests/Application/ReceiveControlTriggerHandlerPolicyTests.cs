using ChronoFlow.Modules.ControlTriggers.Application;
using ChronoFlow.Modules.ControlTriggers.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using static ChronoFlow.Modules.ControlTriggers.Application.OrchestrationPolicyOutcomes;

namespace ChronoFlow.Tests.Application;

public sealed class ReceiveControlTriggerHandlerPolicyTests
{
    private static ReceiveControlTriggerHandler CreateSut(
        IWorkflowExecutor executor,
        IControlExecutionRecordRepository repo) =>
        new(
            NullLogger<ReceiveControlTriggerHandler>.Instance,
            ControlTriggerHandlerTestDefaults.DisabledAdvisoryOptions,
            new DefaultControlTriggerDeduplicator(new InMemoryProcessedTriggerStore()),
            new DefaultControlTriggerRouter(),
            new NoOpControlDecisionAdvisor(),
            new DefaultWorkflowExecutionPolicy(),
            executor,
            repo);

    private static ReceiveControlTriggerCommand CommandWithInboundReason(string reasonCode) =>
        new(
            "AlertCreated",
            Guid.Parse("a1000000-0000-4000-8000-000000000011"),
            Guid.Parse("b2000000-0000-4000-8000-000000000022"),
            Guid.Parse("c3000000-0000-4000-8000-000000000033"),
            new DateTimeOffset(2026, 4, 6, 10, 0, 0, TimeSpan.Zero),
            "Open",
            "AlertCreated",
            null,
            null,
            null,
            "Rule",
            false,
            null,
            new InboundDecisionContext(null, null, null, reasonCode, null));

    [Fact]
    public async Task Default_path_executes_and_sets_proceed_outcome()
    {
        var counter = new CountingWorkflowExecutor();
        var repo = new InMemoryControlExecutionRecordRepository();
        var sut = CreateSut(counter, repo);
        var command = CommandWithInboundReason("not_a_policy_gate");

        var result = await sut.HandleAsync(command, CancellationToken.None);

        Assert.True(result.WasExecuted);
        Assert.Equal(1, counter.CallCount);
        Assert.Equal(Proceed, result.OrchestrationPolicyOutcome);
        var row = repo.Records.Single(r => r.Id == result.ExecutionRecordId);
        Assert.Equal(Proceed, row.OrchestrationPolicyOutcome);
    }

    [Fact]
    public async Task Advisory_only_records_snapshot_without_invoking_executor()
    {
        var counter = new CountingWorkflowExecutor();
        var repo = new InMemoryControlExecutionRecordRepository();
        var sut = CreateSut(counter, repo);
        var command = CommandWithInboundReason(OrchestrationPolicyInboundReasonCodes.AdvisoryOnly);

        var result = await sut.HandleAsync(command, CancellationToken.None);

        Assert.False(result.WasExecuted);
        Assert.Equal(0, counter.CallCount);
        Assert.Equal(AdvisoryOnly, result.OrchestrationPolicyOutcome);
        Assert.Equal("alert-created-default", result.WorkflowKey);
        var row = repo.Records.Single(r => r.Id == result.ExecutionRecordId);
        Assert.False(row.WasExecuted);
        Assert.Equal("alert-created-default", row.WorkflowKey);
        Assert.Equal(AdvisoryOnly, row.OrchestrationPolicyOutcome);
        Assert.False(row.PendingOperatorReview);
    }

    [Fact]
    public async Task Policy_suppress_persists_without_executor()
    {
        var counter = new CountingWorkflowExecutor();
        var repo = new InMemoryControlExecutionRecordRepository();
        var sut = CreateSut(counter, repo);
        var command = CommandWithInboundReason(OrchestrationPolicyInboundReasonCodes.SuppressExecution);

        var result = await sut.HandleAsync(command, CancellationToken.None);

        Assert.False(result.WasExecuted);
        Assert.Equal(0, counter.CallCount);
        Assert.Equal(PolicySuppressed, result.OrchestrationPolicyOutcome);
        var row = repo.Records.Single(r => r.Id == result.ExecutionRecordId);
        Assert.Equal(PolicySuppressed, row.OrchestrationPolicyOutcome);
        Assert.Null(row.WorkflowKey);
        Assert.False(row.WasSuppressed);
    }

    [Fact]
    public async Task Require_review_persists_pending_flag_and_instance_without_executor()
    {
        var counter = new CountingWorkflowExecutor();
        var repo = new InMemoryControlExecutionRecordRepository();
        var sut = CreateSut(counter, repo);
        var command = CommandWithInboundReason(OrchestrationPolicyInboundReasonCodes.RequireReview);

        var result = await sut.HandleAsync(command, CancellationToken.None);

        Assert.False(result.WasExecuted);
        Assert.Equal(0, counter.CallCount);
        Assert.True(result.PendingOperatorReview);
        Assert.Equal(PendingReview, result.OrchestrationPolicyOutcome);
        Assert.NotNull(result.ExecutionInstanceId);
        var row = repo.Records.Single(r => r.Id == result.ExecutionRecordId);
        Assert.True(row.PendingOperatorReview);
        Assert.Equal(PendingReview, row.OrchestrationPolicyOutcome);
        Assert.Equal(result.ExecutionInstanceId, row.ExecutionInstanceId);
        Assert.Equal("alert-created-default", row.WorkflowKey);
    }

    private sealed class CountingWorkflowExecutor : IWorkflowExecutor
    {
        public int CallCount { get; private set; }

        public Task<WorkflowExecutionResult> ExecuteAsync(
            Guid executionInstanceId,
            WorkflowDefinition definition,
            ReceiveControlTriggerCommand trigger,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            _ = trigger;
            _ = cancellationToken;
            return Task.FromResult(new WorkflowExecutionResult(definition.Steps.Count, executionInstanceId));
        }
    }
}
