using ChronoFlow.Modules.ControlTriggers.Application;
using ChronoFlow.Modules.ControlTriggers.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ChronoFlow.Tests.Application;

public sealed class ReceiveControlTriggerHandlerAdvisoryTests
{
    private static ReceiveControlTriggerHandler CreateSut(
        ControlTriggersAdvisoryOptions advisoryOptions,
        IControlDecisionAdvisor advisor,
        IWorkflowExecutor? executor = null,
        IControlExecutionRecordRepository? executionRecords = null,
        IProcessedTriggerStore? store = null) =>
        new(
            NullLogger<ReceiveControlTriggerHandler>.Instance,
            Options.Create(advisoryOptions),
            new DefaultControlTriggerDeduplicator(store ?? new InMemoryProcessedTriggerStore()),
            new DefaultControlTriggerRouter(),
            advisor,
            executor ?? new CountingWorkflowExecutor(),
            executionRecords ?? new InMemoryControlExecutionRecordRepository());

    [Fact]
    public async Task When_advisory_enabled_invokes_advisor_for_routable_trigger()
    {
        var opts = new ControlTriggersAdvisoryOptions { Enabled = true };
        var fake = new FakeControlDecisionAdvisor(
            new ControlAdvisoryOutcome("default_safe", "High", "ok", false, 0));
        var sut = CreateSut(opts, fake);

        var command = ValidCommand("AlertCreated", "AlertCreated");
        var result = await sut.HandleAsync(command, CancellationToken.None);

        Assert.Equal(1, fake.CallCount);
        Assert.True(result.WasExecuted);
    }

    [Fact]
    public async Task When_advisory_disabled_does_not_invoke_advisor()
    {
        var opts = new ControlTriggersAdvisoryOptions { Enabled = false };
        var fake = new FakeControlDecisionAdvisor(
            new ControlAdvisoryOutcome(AdvisoryStrategyKeys.MemoryInformed, "High", "x", false, 0));
        var sut = CreateSut(opts, fake);

        var command = ValidCommand("AlertCreated", "AlertCreated");
        _ = await sut.HandleAsync(command, CancellationToken.None);

        Assert.Equal(0, fake.CallCount);
    }

    [Fact]
    public async Task Suppressed_duplicate_does_not_invoke_advisor()
    {
        var store = new InMemoryProcessedTriggerStore();
        var opts = new ControlTriggersAdvisoryOptions { Enabled = true };
        var fake = new FakeControlDecisionAdvisor(
            new ControlAdvisoryOutcome(AdvisoryStrategyKeys.ContextEscalated, "High", "esc", false, 0));
        var sut = CreateSut(opts, fake, store: store);
        var command = ValidCommand("AlertCreated", "AlertCreated");

        await sut.HandleAsync(command, CancellationToken.None);
        var second = await sut.HandleAsync(command, CancellationToken.None);

        Assert.True(second.WasSuppressed);
        Assert.Equal(1, fake.CallCount);
    }

    [Fact]
    public async Task No_preliminary_workflow_skips_advisor_AlertAcknowledged()
    {
        var opts = new ControlTriggersAdvisoryOptions { Enabled = true };
        var fake = new FakeControlDecisionAdvisor(
            new ControlAdvisoryOutcome(AdvisoryStrategyKeys.MemoryInformed, "High", "x", false, 0));
        var sut = CreateSut(opts, fake);
        var command = ValidCommand("AlertAcknowledged", "AlertAcknowledged");

        _ = await sut.HandleAsync(command, CancellationToken.None);

        Assert.Equal(0, fake.CallCount);
    }

    [Fact]
    public async Task Memory_informed_strategy_selects_distinct_workflow()
    {
        var opts = new ControlTriggersAdvisoryOptions { Enabled = true };
        var fake = new FakeControlDecisionAdvisor(
            new ControlAdvisoryOutcome(AdvisoryStrategyKeys.MemoryInformed, "High", "mem", false, 0));
        var sut = CreateSut(opts, fake);
        var command = ValidCommand("AlertCreated", "AlertCreated");

        var result = await sut.HandleAsync(command, CancellationToken.None);

        Assert.True(result.WasExecuted);
        Assert.Equal("alert-created-memory-informed", result.WorkflowKey);
    }

    [Fact]
    public async Task Unknown_strategy_from_advisor_falls_back_to_default_workflow()
    {
        var opts = new ControlTriggersAdvisoryOptions { Enabled = true };
        var fake = new FakeControlDecisionAdvisor(
            new ControlAdvisoryOutcome("unknown_custom", "Low", "?", false, 0));
        var sut = CreateSut(opts, fake);
        var command = ValidCommand("AlertCreated", "AlertCreated");

        var result = await sut.HandleAsync(command, CancellationToken.None);

        Assert.Equal("alert-created-default", result.WorkflowKey);
    }

    [Fact]
    public async Task Advisory_unavailable_still_executes_default_and_record_has_no_advisory()
    {
        var opts = new ControlTriggersAdvisoryOptions { Enabled = true };
        var fake = new FakeControlDecisionAdvisor(new AdvisoryExecutionResult.Unavailable("test_unavailable"));
        var repo = new InMemoryControlExecutionRecordRepository();
        var sut = CreateSut(opts, fake, executionRecords: repo);
        var command = ValidCommand("AlertCreated", "AlertCreated");

        var result = await sut.HandleAsync(command, CancellationToken.None);

        Assert.True(result.WasExecuted);
        Assert.Equal("alert-created-default", result.WorkflowKey);
        var row = repo.Records.Single(r => r.Id == result.ExecutionRecordId);
        Assert.False(row.AdvisoryWasUsed);
        Assert.Equal(result.ExecutionInstanceId, row.ExecutionInstanceId);
    }

    [Fact]
    public async Task Orchestration_execution_instance_id_matches_advisor_input_and_persisted_record()
    {
        var opts = new ControlTriggersAdvisoryOptions { Enabled = true };
        var fake = new FakeControlDecisionAdvisor(
            new ControlAdvisoryOutcome("default_safe", "High", "ok", false, 0));
        var repo = new InMemoryControlExecutionRecordRepository();
        var sut = CreateSut(opts, fake, executionRecords: repo);
        var command = ValidCommand("AlertCreated", "AlertCreated");

        var result = await sut.HandleAsync(command, CancellationToken.None);

        Assert.NotNull(fake.LastOrchestrationExecutionInstanceId);
        Assert.Equal(fake.LastOrchestrationExecutionInstanceId, result.ExecutionInstanceId);
        var row = repo.Records.Single(r => r.Id == result.ExecutionRecordId);
        Assert.Equal(result.ExecutionInstanceId, row.ExecutionInstanceId);
    }

    [Fact]
    public async Task When_advisory_returns_outcome_executed_record_persists_advisory_fields()
    {
        var opts = new ControlTriggersAdvisoryOptions { Enabled = true };
        var fake = new FakeControlDecisionAdvisor(
            new ControlAdvisoryOutcome(AdvisoryStrategyKeys.DefaultSafe, "Medium", "reason text", false, 0));
        var repo = new InMemoryControlExecutionRecordRepository();
        var sut = CreateSut(opts, fake, executionRecords: repo);
        var command = ValidCommand("AlertCreated", "AlertCreated");

        var result = await sut.HandleAsync(command, CancellationToken.None);

        var row = repo.Records.Single(r => r.Id == result.ExecutionRecordId);
        Assert.True(row.AdvisoryWasUsed);
        Assert.Equal(AdvisoryStrategyKeys.DefaultSafe, row.AdvisoryStrategyKey);
        Assert.Equal("Medium", row.AdvisoryConfidence);
        Assert.Equal("reason text", row.AdvisoryReasonSummary);
        Assert.Equal(result.ExecutionInstanceId, row.ExecutionInstanceId);
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
