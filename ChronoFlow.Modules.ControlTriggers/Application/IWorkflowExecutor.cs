using ChronoFlow.Modules.ControlTriggers.Domain;

namespace ChronoFlow.Modules.ControlTriggers.Application;

/// <summary>Runs an in-memory workflow for a control trigger (orchestration-local; no transport).</summary>
public interface IWorkflowExecutor
{
    Task<WorkflowExecutionResult> ExecuteAsync(
        WorkflowDefinition definition,
        ReceiveControlTriggerCommand trigger,
        CancellationToken cancellationToken = default);
}
