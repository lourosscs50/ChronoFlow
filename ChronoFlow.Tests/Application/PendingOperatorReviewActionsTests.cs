using ChronoFlow.Modules.ControlTriggers.Application;
using ChronoFlow.Modules.ControlTriggers.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using static ChronoFlow.Modules.ControlTriggers.Application.OrchestrationPolicyInboundReasonCodes;
using static ChronoFlow.Modules.ControlTriggers.Application.OrchestrationPolicyOutcomes;

namespace ChronoFlow.Tests.Application;

public sealed class PendingOperatorReviewActionsTests
{
    private static ReceiveControlTriggerHandler CreateReceiveHandler(
        IWorkflowExecutor executor,
        IControlExecutionRecordRepository repo,
        IControlTriggerDeduplicator dedupe) =>
        new(
            NullLogger<ReceiveControlTriggerHandler>.Instance,
            ControlTriggerHandlerTestDefaults.DisabledAdvisoryOptions,
            dedupe,
            new DefaultControlTriggerRouter(),
            new NoOpControlDecisionAdvisor(),
            new DefaultWorkflowExecutionPolicy(),
            executor,
            repo);

    private static ReceiveControlTriggerCommand CommandPendingReview(
        Guid alertId,
        string? correlation = null,
        string? inboundSummary = null) =>
        new(
            "AlertCreated",
            alertId,
            Guid.Parse("b2000000-0000-4000-8000-000000000022"),
            Guid.Parse("c3000000-0000-4000-8000-000000000033"),
            new DateTimeOffset(2026, 4, 6, 11, 0, 0, TimeSpan.Zero),
            "Open",
            "AlertCreated",
            null,
            null,
            null,
            "Rule",
            false,
            correlation,
            new InboundDecisionContext(inboundSummary, null, null, RequireReview, null));

    [Fact]
    public async Task Approve_pending_review_invokes_executor_once_records_dedupe_and_preserves_snapshots()
    {
        var store = new InMemoryProcessedTriggerStore();
        var dedupe = new DefaultControlTriggerDeduplicator(store);
        var repo = new InMemoryControlExecutionRecordRepository();
        var counter = new CountingWorkflowExecutor();
        var receive = CreateReceiveHandler(counter, repo, dedupe);
        var alertId = Guid.Parse("a1000000-0000-4000-8000-000000000099");
        var cmd = CommandPendingReview(alertId, correlation: "corr-p5", inboundSummary: "inbound-snap");

        var intake = await receive.HandleAsync(cmd, CancellationToken.None);
        Assert.True(intake.PendingOperatorReview);
        Assert.Equal(0, counter.CallCount);
        var pendingRow = repo.Records.Single();
        Assert.True(pendingRow.PendingOperatorReview);
        Assert.Equal(PendingReview, pendingRow.OrchestrationPolicyOutcome);
        Assert.Equal("inbound-snap", pendingRow.InboundDecisionSummary);
        Assert.Equal("corr-p5", pendingRow.CorrelationId);

        var approve = new ApprovePendingControlExecution.Handler(
            repo,
            new DefaultControlTriggerRouter(),
            counter,
            dedupe);

        var result = await approve.HandleAsync(pendingRow.Id, " proceed ", CancellationToken.None);
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Record);

        var updated = repo.Records.Single();
        Assert.True(updated.WasExecuted);
        Assert.False(updated.PendingOperatorReview);
        Assert.Equal(Proceed, updated.OrchestrationPolicyOutcome);
        Assert.Equal(OperatorReviewActions.Approved, updated.OperatorReviewAction);
        Assert.NotNull(updated.OperatorReviewActionAtUtc);
        Assert.Equal("proceed", updated.OperatorReviewNote);
        Assert.Equal("inbound-snap", updated.InboundDecisionSummary);
        Assert.Equal("corr-p5", updated.CorrelationId);
        Assert.Equal(new DateTimeOffset(2026, 4, 6, 11, 0, 0, TimeSpan.Zero), updated.OccurredAtUtc);
        Assert.Equal(1, counter.CallCount);

        var replay = await receive.HandleAsync(cmd, CancellationToken.None);
        Assert.True(replay.WasSuppressed);
        Assert.Equal(1, counter.CallCount);
    }

    [Fact]
    public async Task Cancel_pending_review_does_not_invoke_executor_and_sets_review_cancelled()
    {
        var repo = new InMemoryControlExecutionRecordRepository();
        var counter = new CountingWorkflowExecutor();
        var receive = CreateReceiveHandler(
            counter,
            repo,
            new DefaultControlTriggerDeduplicator(new InMemoryProcessedTriggerStore()));

        var cmd = CommandPendingReview(Guid.Parse("a1000000-0000-4000-8000-000000000088"));
        var intake = await receive.HandleAsync(cmd, CancellationToken.None);
        Assert.True(intake.PendingOperatorReview);
        var pendingId = intake.ExecutionRecordId!.Value;

        var cancel = new CancelPendingControlExecution.Handler(repo);
        var result = await cancel.HandleAsync(pendingId, "no-go", CancellationToken.None);
        Assert.True(result.Succeeded);

        var row = repo.Records.Single();
        Assert.False(row.WasExecuted);
        Assert.False(row.PendingOperatorReview);
        Assert.Equal(ReviewCancelled, row.OrchestrationPolicyOutcome);
        Assert.Equal(OperatorReviewActions.Cancelled, row.OperatorReviewAction);
        Assert.Equal("no-go", row.OperatorReviewNote);
        Assert.Equal(0, counter.CallCount);

        var secondCancel = await cancel.HandleAsync(pendingId, null, CancellationToken.None);
        Assert.False(secondCancel.Succeeded);
        Assert.Equal(PendingReviewActionFailureKind.NotPendingReview, secondCancel.Failure);
    }

    [Fact]
    public async Task Repeated_or_invalid_review_actions_are_rejected_without_extra_execution()
    {
        var repo = new InMemoryControlExecutionRecordRepository();
        var counter = new CountingWorkflowExecutor();
        var dedupe = new DefaultControlTriggerDeduplicator(new InMemoryProcessedTriggerStore());
        var receive = CreateReceiveHandler(counter, repo, dedupe);
        var cmd = CommandPendingReview(Guid.Parse("a1000000-0000-4000-8000-000000000077"));

        var intake = await receive.HandleAsync(cmd, CancellationToken.None);
        var pendingId = intake.ExecutionRecordId!.Value;

        var approve = new ApprovePendingControlExecution.Handler(
            repo,
            new DefaultControlTriggerRouter(),
            counter,
            dedupe);

        Assert.True((await approve.HandleAsync(pendingId, null, CancellationToken.None)).Succeeded);
        Assert.Equal(1, counter.CallCount);

        var secondApprove = await approve.HandleAsync(pendingId, null, CancellationToken.None);
        Assert.False(secondApprove.Succeeded);
        Assert.Equal(PendingReviewActionFailureKind.NotPendingReview, secondApprove.Failure);
        Assert.Equal(1, counter.CallCount);

        var wrongId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
        var missing = await approve.HandleAsync(wrongId, null, CancellationToken.None);
        Assert.False(missing.Succeeded);
        Assert.Equal(PendingReviewActionFailureKind.NotFound, missing.Failure);

        var repo2 = new InMemoryControlExecutionRecordRepository();
        await repo2.AddAsync(
            new ControlExecutionRecord
            {
                Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                ExecutionInstanceId = null,
                TriggerType = "AlertCreated",
                LifecycleEventType = "AlertCreated",
                AlertId = Guid.NewGuid(),
                RuleId = Guid.NewGuid(),
                SignalId = Guid.NewGuid(),
                CorrelationId = null,
                OccurredAtUtc = null,
                WorkflowKey = "alert-created-default",
                WasExecuted = false,
                WasSuppressed = false,
                SuppressionReason = null,
                ExecutedStepCount = 0,
                ReceivedAtUtc = DateTimeOffset.UtcNow,
                ExecutedAtUtc = null,
                CurrentStatus = "Open",
                RuleName = null,
                HasBeenReopened = false,
                AdvisoryWasUsed = false,
                PendingOperatorReview = true,
                OrchestrationPolicyOutcome = PendingReview,
                OperatorReviewAction = null,
                OperatorReviewActionAtUtc = null,
                OperatorReviewNote = null
            },
            CancellationToken.None);

        var approveForRepo2 = new ApprovePendingControlExecution.Handler(
            repo2,
            new DefaultControlTriggerRouter(),
            counter,
            dedupe);
        var badState = await approveForRepo2.HandleAsync(
            Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
            null,
            CancellationToken.None);
        Assert.False(badState.Succeeded);
        Assert.Equal(PendingReviewActionFailureKind.InvalidRecordState, badState.Failure);
        Assert.Equal(1, counter.CallCount);
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
