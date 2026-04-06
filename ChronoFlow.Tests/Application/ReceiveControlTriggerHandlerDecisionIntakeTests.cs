using ChronoFlow.Modules.ControlTriggers.Application;
using ChronoFlow.Modules.ControlTriggers.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ChronoFlow.Tests.Application;

/// <summary>Phase 2: decision-aware intake snapshot semantics (bounded, truthful, start-time).</summary>
public sealed class ReceiveControlTriggerHandlerDecisionIntakeTests
{
    private static ReceiveControlTriggerHandler CreateSut(
        ControlTriggersAdvisoryOptions advisoryOptions,
        IControlDecisionAdvisor advisor,
        IControlExecutionRecordRepository repo) =>
        new(
            NullLogger<ReceiveControlTriggerHandler>.Instance,
            Options.Create(advisoryOptions),
            new DefaultControlTriggerDeduplicator(new InMemoryProcessedTriggerStore()),
            new DefaultControlTriggerRouter(),
            advisor,
            new DefaultWorkflowExecutionPolicy(),
            new CountingWorkflowExecutor(),
            repo);

    [Fact]
    public async Task When_inbound_decision_present_and_advisory_succeeds_start_snapshot_persists_both_truthfully()
    {
        var opts = new ControlTriggersAdvisoryOptions { Enabled = true };
        var fake = new FakeControlDecisionAdvisor(
            new ControlAdvisoryOutcome(
                "default_safe",
                "High",
                "ail reason",
                false,
                0,
                LinkedAilExecutionId: "ail-linked-42"));
        var repo = new InMemoryControlExecutionRecordRepository();
        var sut = CreateSut(opts, fake, repo);
        var command = ValidCommand("AlertCreated", "AlertCreated") with
        {
            InboundDecision = new InboundDecisionContext(
                "Upstream decision summary",
                "ext-ref-aa",
                "Medium",
                "SF_ESCALATE",
                "ext-run-9")
        };

        var result = await sut.HandleAsync(command, CancellationToken.None);

        Assert.True(result.WasExecuted);
        var row = repo.Records.Single(r => r.Id == result.ExecutionRecordId);
        Assert.True(row.AdvisoryWasUsed);
        Assert.Equal("default_safe", row.AdvisoryStrategyKey);
        Assert.Equal("High", row.AdvisoryConfidence);
        Assert.Equal("ail reason", row.AdvisoryReasonSummary);
        Assert.Equal("ail-linked-42", row.LinkedAilExecutionId);
        Assert.Equal("Upstream decision summary", row.InboundDecisionSummary);
        Assert.Equal("ext-ref-aa", row.InboundDecisionReferenceId);
        Assert.Equal("Medium", row.InboundDecisionConfidence);
        Assert.Equal("SF_ESCALATE", row.InboundDecisionReasonCode);
        Assert.Equal("ext-run-9", row.InboundLinkedExternalExecutionId);
    }

    [Fact]
    public async Task When_no_inbound_and_advisory_unsuccessful_no_false_advisory_or_inbound_context()
    {
        var opts = new ControlTriggersAdvisoryOptions { Enabled = true };
        var fake = new FakeControlDecisionAdvisor(new AdvisoryExecutionResult.Unavailable("test"));
        var repo = new InMemoryControlExecutionRecordRepository();
        var sut = CreateSut(opts, fake, repo);
        var command = ValidCommand("AlertCreated", "AlertCreated");

        var result = await sut.HandleAsync(command, CancellationToken.None);

        Assert.True(result.WasExecuted);
        var row = repo.Records.Single(r => r.Id == result.ExecutionRecordId);
        Assert.False(row.AdvisoryWasUsed);
        Assert.Null(row.AdvisoryStrategyKey);
        Assert.Null(row.LinkedAilExecutionId);
        Assert.Null(row.InboundDecisionSummary);
        Assert.Null(row.InboundDecisionReferenceId);
        Assert.Null(row.InboundDecisionConfidence);
        Assert.Null(row.InboundDecisionReasonCode);
        Assert.Null(row.InboundLinkedExternalExecutionId);
    }

    [Fact]
    public async Task Inbound_strings_are_bounded_at_snapshot_time_second_execution_does_not_mutate_first_record()
    {
        var opts = new ControlTriggersAdvisoryOptions { Enabled = false };
        var fake = new FakeControlDecisionAdvisor(
            new ControlAdvisoryOutcome("x", "Low", "y", false, 0));
        var repo = new InMemoryControlExecutionRecordRepository();
        var sut = CreateSut(opts, fake, repo);
        var longSummary = new string('z', InboundDecisionIntakeMapper.MaxSummaryLength + 120);
        var cmd1 = ValidCommand("AlertCreated", "AlertCreated") with
        {
            AlertId = Guid.Parse("a1000000-0000-4000-8000-000000000001"),
            InboundDecision = new InboundDecisionContext(longSummary, "ref-a", null, null, null)
        };
        var cmd2 = ValidCommand("AlertCreated", "AlertCreated") with
        {
            AlertId = Guid.Parse("a2000000-0000-4000-8000-000000000002"),
            InboundDecision = new InboundDecisionContext("second", "ref-b", null, null, null)
        };

        var first = await sut.HandleAsync(cmd1, CancellationToken.None);
        var second = await sut.HandleAsync(cmd2, CancellationToken.None);

        var row1 = repo.Records.Single(r => r.Id == first.ExecutionRecordId);
        var row2 = repo.Records.Single(r => r.Id == second.ExecutionRecordId);
        Assert.Equal(InboundDecisionIntakeMapper.MaxSummaryLength, row1.InboundDecisionSummary!.Length);
        Assert.All(row1.InboundDecisionSummary, c => Assert.Equal('z', c));
        Assert.Equal("second", row2.InboundDecisionSummary);
        Assert.Equal("ref-a", row1.InboundDecisionReferenceId);
        Assert.Equal("ref-b", row2.InboundDecisionReferenceId);
    }

    private static ReceiveControlTriggerCommand ValidCommand(string triggerType, string lifecycle) =>
        new(
            triggerType,
            Guid.Parse("a1000000-0000-0000-0000-000000000011"),
            Guid.Parse("b2000000-0000-0000-0000-000000000022"),
            Guid.Parse("c3000000-0000-0000-0000-000000000033"),
            new DateTimeOffset(2026, 4, 3, 11, 0, 0, TimeSpan.Zero),
            "Open",
            lifecycle,
            null,
            null,
            null,
            "Rule",
            false);

    private sealed class CountingWorkflowExecutor : IWorkflowExecutor
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
