using ChronoFlow.Modules.ControlTriggers.Application;
using ChronoFlow.Modules.ControlTriggers.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ChronoFlow.Tests.Application;

/// <summary>Phase 4: durable correlation and honest absence of invented linkage.</summary>
public sealed class ControlExecutionTraceHonestyTests
{
    private static ReceiveControlTriggerHandler CreateSut(IControlExecutionRecordRepository repo) =>
        new(
            NullLogger<ReceiveControlTriggerHandler>.Instance,
            ControlTriggerHandlerTestDefaults.DisabledAdvisoryOptions,
            new DefaultControlTriggerDeduplicator(new InMemoryProcessedTriggerStore()),
            new DefaultControlTriggerRouter(),
            new NoOpControlDecisionAdvisor(),
            new DefaultWorkflowExecutionPolicy(),
            new RecordingWorkflowExecutor(),
            repo);

    private static ReceiveControlTriggerCommand BaseCommand() =>
        new(
            "AlertCreated",
            Guid.Parse("a1000000-0000-4000-8000-000000000011"),
            Guid.Parse("b2000000-0000-4000-8000-000000000022"),
            Guid.Parse("c3000000-0000-4000-8000-000000000033"),
            new DateTimeOffset(2026, 4, 6, 12, 0, 0, TimeSpan.Zero),
            "Open",
            "AlertCreated",
            null,
            null,
            null,
            "Rule",
            false);

    [Fact]
    public async Task Without_correlation_intake_persists_null_not_synthetic_value()
    {
        var repo = new InMemoryControlExecutionRecordRepository();
        var sut = CreateSut(repo);
        var result = await sut.HandleAsync(BaseCommand(), CancellationToken.None);

        var row = repo.Records.Single(r => r.Id == result.ExecutionRecordId);
        Assert.Null(row.CorrelationId);
        Assert.Null(row.LinkedAilExecutionId);
        Assert.False(row.AdvisoryWasUsed);
    }

    [Fact]
    public async Task With_correlation_intake_persists_bounded_copy_on_executed_record()
    {
        var repo = new InMemoryControlExecutionRecordRepository();
        var sut = CreateSut(repo);
        var cmd = BaseCommand() with { CorrelationId = "corr-handler-99" };
        var result = await sut.HandleAsync(cmd, CancellationToken.None);

        var row = repo.Records.Single(r => r.Id == result.ExecutionRecordId);
        Assert.Equal("corr-handler-99", row.CorrelationId);
    }

    private sealed class RecordingWorkflowExecutor : IWorkflowExecutor
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
