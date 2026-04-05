using ChronoFlow.Modules.ControlTriggers.Application;
using ChronoFlow.Modules.ControlTriggers.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ChronoFlow.Tests.Application;

/// <summary>Phase 6.5–6.6: intake → deduplicate → route → execute semantics.</summary>
public sealed class ReceiveControlTriggerHandlerExecutionTests
{
    private static ReceiveControlTriggerHandler CreateSut(
        IControlTriggerRouter router,
        IWorkflowExecutor executor,
        IControlTriggerDeduplicator? deduplicator = null,
        IControlExecutionRecordRepository? executionRecords = null) =>
        new(
            NullLogger<ReceiveControlTriggerHandler>.Instance,
            ControlTriggerHandlerTestDefaults.DisabledAdvisoryOptions,
            deduplicator ?? new DefaultControlTriggerDeduplicator(new InMemoryProcessedTriggerStore()),
            router,
            new NoOpControlDecisionAdvisor(),
            executor,
            executionRecords ?? new InMemoryControlExecutionRecordRepository());

    [Fact]
    public async Task AlertCreated_routes_to_default_workflow_and_executes()
    {
        var recorder = new RecordingWorkflowExecutor();
        var sut = CreateSut(new DefaultControlTriggerRouter(), recorder);
        var command = ValidCommand("AlertCreated", "AlertCreated");

        var result = await sut.HandleAsync(command, CancellationToken.None);

        Assert.True(result.Accepted);
        Assert.True(result.WasExecuted);
        Assert.False(result.WasSuppressed);
        Assert.Equal("alert-created-default", result.WorkflowKey);
        Assert.Equal(3, result.ExecutedStepCount);
        Assert.Equal(
            ["EvaluateTrigger", "InitializeWorkflowContext", "RecordExecution"],
            recorder.StepNamesInOrder);
        Assert.NotEqual(Guid.Empty, result.ExecutionInstanceId);
        Assert.Equal(result.ExecutionInstanceId, recorder.LastExecutionInstanceId);
    }

    [Fact]
    public async Task AlertReopened_routes_to_default_workflow_and_executes()
    {
        var recorder = new RecordingWorkflowExecutor();
        var sut = CreateSut(new DefaultControlTriggerRouter(), recorder);
        var command = ValidCommand("AlertReopened", "AlertReopened");

        var result = await sut.HandleAsync(command, CancellationToken.None);

        Assert.True(result.WasExecuted);
        Assert.False(result.WasSuppressed);
        Assert.Equal("alert-reopened-default", result.WorkflowKey);
        Assert.Equal(
            ["EvaluateTrigger", "EscalationCheck", "RecordExecution"],
            recorder.StepNamesInOrder);
    }

    [Fact]
    public async Task AlertAcknowledged_is_accepted_without_execution()
    {
        var recorder = new RecordingWorkflowExecutor();
        var sut = CreateSut(new DefaultControlTriggerRouter(), recorder);
        var command = ValidCommand("AlertAcknowledged", "AlertAcknowledged");

        var result = await sut.HandleAsync(command, CancellationToken.None);

        Assert.True(result.Accepted);
        Assert.False(result.WasExecuted);
        Assert.False(result.WasSuppressed);
        Assert.Null(result.WorkflowKey);
        Assert.Equal(0, result.ExecutedStepCount);
        Assert.Null(result.ExecutionInstanceId);
        Assert.Empty(recorder.StepNamesInOrder);
    }

    [Fact]
    public async Task AlertResolved_is_accepted_without_execution()
    {
        var recorder = new RecordingWorkflowExecutor();
        var sut = CreateSut(new DefaultControlTriggerRouter(), recorder);
        var command = ValidCommand("AlertResolved", "AlertResolved");

        var result = await sut.HandleAsync(command, CancellationToken.None);

        Assert.True(result.Accepted);
        Assert.False(result.WasExecuted);
        Assert.False(result.WasSuppressed);
        Assert.Null(result.ExecutionInstanceId);
        Assert.Empty(recorder.StepNamesInOrder);
    }

    [Fact]
    public async Task Null_router_does_not_crash_and_skips_execution()
    {
        var recorder = new RecordingWorkflowExecutor();
        var sut = CreateSut(new NullControlTriggerRouter(), recorder);
        var command = ValidCommand("AlertCreated", "AlertCreated");

        var result = await sut.HandleAsync(command, CancellationToken.None);

        Assert.True(result.Accepted);
        Assert.False(result.WasExecuted);
        Assert.False(result.WasSuppressed);
        Assert.Null(result.ExecutionInstanceId);
        Assert.Empty(recorder.StepNamesInOrder);
    }

    [Fact]
    public async Task Execution_result_reflects_step_count_from_executor()
    {
        var sut = CreateSut(
            new DefaultControlTriggerRouter(),
            new FixedCountWorkflowExecutor(executedStepCount: 99));
        var command = ValidCommand("AlertCreated", "AlertCreated");

        var result = await sut.HandleAsync(command, CancellationToken.None);

        Assert.True(result.WasExecuted);
        Assert.Equal(99, result.ExecutedStepCount);
        Assert.NotEqual(Guid.Empty, result.ExecutionInstanceId);
    }

    private static ReceiveControlTriggerCommand ValidCommand(string triggerType, string lifecycle) =>
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

    private sealed class RecordingWorkflowExecutor : IWorkflowExecutor
    {
        public List<string> StepNamesInOrder { get; } = [];

        public Guid LastExecutionInstanceId { get; private set; }

        public Task<WorkflowExecutionResult> ExecuteAsync(
            Guid executionInstanceId,
            WorkflowDefinition definition,
            ReceiveControlTriggerCommand trigger,
            CancellationToken cancellationToken = default)
        {
            _ = trigger;
            _ = cancellationToken;
            LastExecutionInstanceId = executionInstanceId;
            foreach (var step in definition.Steps.OrderBy(s => s.Order))
                StepNamesInOrder.Add(step.StepName);
            return Task.FromResult(new WorkflowExecutionResult(definition.Steps.Count, executionInstanceId));
        }
    }

    private sealed class NullControlTriggerRouter : IControlTriggerRouter
    {
        public WorkflowDefinition? ResolveWorkflow(
            ReceiveControlTriggerCommand command,
            ControlAdvisoryRouteHint? advisoryHint = null) => null;
    }

    private sealed class FixedCountWorkflowExecutor(int executedStepCount) : IWorkflowExecutor
    {
        public Task<WorkflowExecutionResult> ExecuteAsync(
            Guid executionInstanceId,
            WorkflowDefinition definition,
            ReceiveControlTriggerCommand trigger,
            CancellationToken cancellationToken = default)
        {
            _ = definition;
            _ = trigger;
            _ = cancellationToken;
            return Task.FromResult(new WorkflowExecutionResult(executedStepCount, executionInstanceId));
        }
    }
}
