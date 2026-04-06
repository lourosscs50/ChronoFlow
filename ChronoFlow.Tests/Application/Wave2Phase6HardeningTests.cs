using ChronoFlow.Modules.ControlTriggers.Application;
using ChronoFlow.Modules.ControlTriggers.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using static ChronoFlow.Modules.ControlTriggers.Application.OrchestrationPolicyInboundReasonCodes;
using static ChronoFlow.Modules.ControlTriggers.Application.OrchestrationPolicyOutcomes;

namespace ChronoFlow.Tests.Application;

/// <summary>Phase 6: Wave 2 closeout — invalid transitions, dedupe integrity, non-HITL rows.</summary>
public sealed class Wave2Phase6HardeningTests
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

    private static ReceiveControlTriggerCommand CommandPendingReview(Guid alertId) =>
        new(
            "AlertCreated",
            alertId,
            Guid.Parse("b2000000-0000-4000-8000-000000000022"),
            Guid.Parse("c3000000-0000-4000-8000-000000000033"),
            new DateTimeOffset(2026, 4, 7, 9, 0, 0, TimeSpan.Zero),
            "Open",
            "AlertCreated",
            null,
            null,
            null,
            "Rule",
            false,
            null,
            new InboundDecisionContext(null, null, null, RequireReview, null));

    [Fact]
    public async Task Approve_after_cancel_is_finalized_without_executor_or_dedupe_side_effects()
    {
        var store = new InMemoryProcessedTriggerStore();
        var countingDedupe = new RecordingDeduplicator(new DefaultControlTriggerDeduplicator(store));
        var repo = new InMemoryControlExecutionRecordRepository();
        var counter = new CountingWorkflowExecutor();
        var receive = CreateReceiveHandler(counter, repo, countingDedupe);
        var alertId = Guid.Parse("a1000000-0000-4000-8000-0000000000a1");
        var intake = await receive.HandleAsync(CommandPendingReview(alertId), CancellationToken.None);
        var id = intake.ExecutionRecordId!.Value;

        var cancel = new CancelPendingControlExecution.Handler(repo);
        Assert.True((await cancel.HandleAsync(id, null, CancellationToken.None)).Succeeded);

        var approve = new ApprovePendingControlExecution.Handler(
            repo,
            new DefaultControlTriggerRouter(),
            counter,
            countingDedupe);

        var denied = await approve.HandleAsync(id, null, CancellationToken.None);
        Assert.False(denied.Succeeded);
        Assert.Equal(PendingReviewActionFailureKind.AlreadyFinalized, denied.Failure);
        Assert.Equal(0, counter.CallCount);
        Assert.Equal(0, countingDedupe.RecordSuccessCalls);
    }

    [Fact]
    public async Task Cancel_after_approve_is_finalized_without_second_persist_mutation()
    {
        var store = new InMemoryProcessedTriggerStore();
        var dedupe = new DefaultControlTriggerDeduplicator(store);
        var repo = new InMemoryControlExecutionRecordRepository();
        var counter = new CountingWorkflowExecutor();
        var receive = CreateReceiveHandler(counter, repo, dedupe);
        var alertId = Guid.Parse("a1000000-0000-4000-8000-0000000000a2");
        var intake = await receive.HandleAsync(CommandPendingReview(alertId), CancellationToken.None);
        var id = intake.ExecutionRecordId!.Value;

        var approve = new ApprovePendingControlExecution.Handler(
            repo,
            new DefaultControlTriggerRouter(),
            counter,
            dedupe);
        Assert.True((await approve.HandleAsync(id, null, CancellationToken.None)).Succeeded);
        var noteAfterApprove = repo.Records.Single().OperatorReviewNote;

        var cancel = new CancelPendingControlExecution.Handler(repo);
        var denied = await cancel.HandleAsync(id, "late-cancel", CancellationToken.None);
        Assert.False(denied.Succeeded);
        Assert.Equal(PendingReviewActionFailureKind.AlreadyFinalized, denied.Failure);

        var row = repo.Records.Single();
        Assert.Equal(OperatorReviewActions.Approved, row.OperatorReviewAction);
        Assert.Equal(noteAfterApprove, row.OperatorReviewNote);
        Assert.Equal(1, counter.CallCount);
    }

    [Fact]
    public async Task Second_approve_does_not_call_RecordSuccessfulExecution_again()
    {
        var store = new InMemoryProcessedTriggerStore();
        var recording = new RecordingDeduplicator(new DefaultControlTriggerDeduplicator(store));
        var repo = new InMemoryControlExecutionRecordRepository();
        var counter = new CountingWorkflowExecutor();
        var receive = CreateReceiveHandler(counter, repo, recording);
        var intake = await receive.HandleAsync(
            CommandPendingReview(Guid.Parse("a1000000-0000-4000-8000-0000000000a3")),
            CancellationToken.None);
        var id = intake.ExecutionRecordId!.Value;

        var approve = new ApprovePendingControlExecution.Handler(
            repo,
            new DefaultControlTriggerRouter(),
            counter,
            recording);

        Assert.True((await approve.HandleAsync(id, null, CancellationToken.None)).Succeeded);
        Assert.Equal(1, recording.RecordSuccessCalls);

        await approve.HandleAsync(id, null, CancellationToken.None);
        Assert.Equal(1, recording.RecordSuccessCalls);
        Assert.Equal(1, counter.CallCount);
    }

    [Fact]
    public async Task Review_actions_reject_advisory_only_and_suppressed_rows()
    {
        var repo = new InMemoryControlExecutionRecordRepository();
        var dedupe = new DefaultControlTriggerDeduplicator(new InMemoryProcessedTriggerStore());
        var counter = new CountingWorkflowExecutor();
        var approve = new ApprovePendingControlExecution.Handler(
            repo,
            new DefaultControlTriggerRouter(),
            counter,
            dedupe);
        var cancel = new CancelPendingControlExecution.Handler(repo);

        var advisoryOnlyId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1");
        await repo.AddAsync(
            SampleRow(advisoryOnlyId, advisoryOnly: true, suppressed: false),
            CancellationToken.None);

        Assert.Equal(
            PendingReviewActionFailureKind.NotPendingReview,
            (await approve.HandleAsync(advisoryOnlyId, null, CancellationToken.None)).Failure);
        Assert.Equal(
            PendingReviewActionFailureKind.NotPendingReview,
            (await cancel.HandleAsync(advisoryOnlyId, null, CancellationToken.None)).Failure);

        var suppressedId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2");
        await repo.AddAsync(
            SampleRow(
                suppressedId,
                advisoryOnly: false,
                suppressed: true),
            CancellationToken.None);

        Assert.Equal(
            PendingReviewActionFailureKind.NotPendingReview,
            (await approve.HandleAsync(suppressedId, null, CancellationToken.None)).Failure);
        Assert.Equal(0, counter.CallCount);
    }

    [Fact]
    public async Task Action_response_matches_persisted_row_for_operator_audit_fields()
    {
        var repo = new InMemoryControlExecutionRecordRepository();
        var dedupe = new DefaultControlTriggerDeduplicator(new InMemoryProcessedTriggerStore());
        var counter = new CountingWorkflowExecutor();
        var receive = CreateReceiveHandler(counter, repo, dedupe);
        var intake = await receive.HandleAsync(
            CommandPendingReview(Guid.Parse("a1000000-0000-4000-8000-0000000000a4")),
            CancellationToken.None);
        var id = intake.ExecutionRecordId!.Value;

        var approve = new ApprovePendingControlExecution.Handler(
            repo,
            new DefaultControlTriggerRouter(),
            counter,
            dedupe);

        var result = await approve.HandleAsync(id, "audit-note", CancellationToken.None);
        Assert.True(result.Succeeded);
        var fromRepo = repo.Records.Single();
        Assert.Equal(fromRepo.OperatorReviewAction, result.Record!.OperatorReviewAction);
        Assert.Equal(fromRepo.OperatorReviewNote, result.Record.OperatorReviewNote);
        Assert.Equal(fromRepo.OrchestrationPolicyOutcome, result.Record.OrchestrationPolicyOutcome);
        Assert.Equal(fromRepo.CorrelationId, result.Record.CorrelationId);
        Assert.Null(fromRepo.LinkedAilExecutionId);
    }

    private static ControlExecutionRecord SampleRow(Guid id, bool advisoryOnly, bool suppressed) =>
        new()
        {
            Id = id,
            ExecutionInstanceId = null,
            TriggerType = "AlertCreated",
            LifecycleEventType = "AlertCreated",
            AlertId = Guid.NewGuid(),
            RuleId = Guid.NewGuid(),
            SignalId = Guid.NewGuid(),
            CorrelationId = null,
            OccurredAtUtc = null,
            WorkflowKey = advisoryOnly ? "alert-created-default" : null,
            WasExecuted = false,
            WasSuppressed = suppressed,
            SuppressionReason = suppressed ? "dup" : null,
            ExecutedStepCount = 0,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
            ExecutedAtUtc = null,
            CurrentStatus = "Open",
            RuleName = null,
            HasBeenReopened = false,
            AdvisoryWasUsed = false,
            PendingOperatorReview = false,
            OrchestrationPolicyOutcome = advisoryOnly ? OrchestrationPolicyOutcomes.AdvisoryOnly : null,
            OperatorReviewAction = null,
            OperatorReviewActionAtUtc = null,
            OperatorReviewNote = null
        };

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

    private sealed class RecordingDeduplicator(IControlTriggerDeduplicator inner) : IControlTriggerDeduplicator
    {
        public int RecordSuccessCalls { get; private set; }

        public ControlTriggerDeduplicationResult EvaluateBeforeRouting(ReceiveControlTriggerCommand command) =>
            inner.EvaluateBeforeRouting(command);

        public void RecordSuccessfulExecution(ReceiveControlTriggerCommand command)
        {
            RecordSuccessCalls++;
            inner.RecordSuccessfulExecution(command);
        }
    }
}
