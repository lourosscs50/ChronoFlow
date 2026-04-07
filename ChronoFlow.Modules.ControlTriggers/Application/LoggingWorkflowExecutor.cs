using ChronoFlow.Modules.ControlTriggers.Domain;
using Microsoft.Extensions.Logging;

namespace ChronoFlow.Modules.ControlTriggers.Application;

/// <summary>Executes workflow steps in <see cref="WorkflowStepDefinition.Order"/> and logs each step (testable, no external IO).</summary>
public sealed class LoggingWorkflowExecutor(ILogger<LoggingWorkflowExecutor> logger) : IWorkflowExecutor
{
    public Task<WorkflowExecutionResult> ExecuteAsync(
        Guid executionInstanceId,
        WorkflowDefinition definition,
        ReceiveControlTriggerCommand trigger,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var ordered = definition.Steps.OrderBy(s => s.Order).ToList();
        foreach (var step in ordered)
        {
            logger.LogInformation(
                "Workflow {WorkflowKey} execution {ExecutionInstanceId} step {StepName} ({StepType}) order {Order} for alert {AlertId}",
                definition.WorkflowKey,
                executionInstanceId,
                step.StepName,
                step.StepType,
                step.Order,
                trigger.AlertId);
        }

        return Task.FromResult(new WorkflowExecutionResult(ordered.Count, executionInstanceId));
    }
}
