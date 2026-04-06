using ChronoFlow.Modules.ControlTriggers.Application;
using static ChronoFlow.Modules.ControlTriggers.Application.OrchestrationPolicyOutcomes;
using ChronoFlow.Modules.ControlTriggers.Domain;
using Xunit;

namespace ChronoFlow.Tests.Application;

public sealed class ListGetControlExecutionHandlersTests
{
    [Fact]
    public async Task List_handler_applies_filters()
    {
        var repo = new InMemoryControlExecutionRecordRepository();
        var idExecuted = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var idOtherAlert = Guid.Parse("22222222-2222-2222-2222-222222222222");
        await repo.AddAsync(MakeRecord(idExecuted, Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), wasExecuted: true, suppressed: false, "AlertCreated"), default);
        await repo.AddAsync(MakeRecord(idOtherAlert, Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), wasExecuted: false, suppressed: true, "AlertCreated"), default);

        var handler = new ListControlExecutions.Handler(repo);
        var forAlert = await handler.HandleAsync(
            new ControlExecutionRecordQuery(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                null,
                null,
                null,
                null,
                0,
                50),
            default);

        Assert.Single(forAlert);
        Assert.Equal(idExecuted, forAlert[0].Id);

        var executedOnly = await handler.HandleAsync(
            new ControlExecutionRecordQuery(null, null, true, null, null, 0, 50),
            default);

        Assert.Single(executedOnly);
        Assert.True(executedOnly[0].WasExecuted);
    }

    [Fact]
    public async Task Get_by_id_returns_null_when_missing()
    {
        var repo = new InMemoryControlExecutionRecordRepository();
        var handler = new GetControlExecutionById.Handler(repo);
        var row = await handler.HandleAsync(Guid.NewGuid(), default);
        Assert.Null(row);
    }

    [Fact]
    public async Task Get_by_id_returns_row()
    {
        var repo = new InMemoryControlExecutionRecordRepository();
        var id = Guid.Parse("33333333-3333-3333-3333-333333333333");
        await repo.AddAsync(MakeRecord(id, Guid.NewGuid(), wasExecuted: true, suppressed: false, "AlertReopened"), default);
        var handler = new GetControlExecutionById.Handler(repo);
        var row = await handler.HandleAsync(id, default);
        Assert.NotNull(row);
        Assert.Equal(id, row!.Id);
    }

    private static ControlExecutionRecord MakeRecord(
        Guid id,
        Guid alertId,
        bool wasExecuted,
        bool suppressed,
        string lifecycle) =>
        new()
        {
            Id = id,
            TriggerType = lifecycle,
            LifecycleEventType = lifecycle,
            AlertId = alertId,
            RuleId = Guid.Parse("b2000000-0000-0000-0000-000000000002"),
            SignalId = Guid.Parse("c3000000-0000-0000-0000-000000000003"),
            WorkflowKey = wasExecuted ? "wf" : null,
            WasExecuted = wasExecuted,
            WasSuppressed = suppressed,
            SuppressionReason = suppressed ? "dup" : null,
            ExecutedStepCount = wasExecuted ? 2 : 0,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
            ExecutedAtUtc = wasExecuted ? DateTimeOffset.UtcNow : null,
            CurrentStatus = "Open",
            HasBeenReopened = false,
            AdvisoryWasUsed = false,
            AdvisoryStrategyKey = null,
            AdvisoryConfidence = null,
            AdvisoryReasonSummary = null,
            LinkedAilExecutionId = null,
            InboundDecisionSummary = null,
            InboundDecisionReferenceId = null,
            InboundDecisionConfidence = null,
            InboundDecisionReasonCode = null,
            InboundLinkedExternalExecutionId = null,
            PendingOperatorReview = false,
            OrchestrationPolicyOutcome = wasExecuted ? Proceed : null
        };
}
