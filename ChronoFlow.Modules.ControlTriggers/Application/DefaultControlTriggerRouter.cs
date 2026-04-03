using ChronoFlow.Modules.ControlTriggers.Domain;

namespace ChronoFlow.Modules.ControlTriggers.Application;

/// <summary>
/// Deterministic in-memory routing: <see cref="ReceiveControlTriggerCommand.LifecycleEventType"/> drives workflow selection.
/// <list type="bullet">
/// <item><description><c>AlertCreated</c> → <c>alert-created-default</c></description></item>
/// <item><description><c>AlertReopened</c> → <c>alert-reopened-default</c></description></item>
/// <item><description><c>AlertAcknowledged</c>, <c>AlertResolved</c>, and others → no workflow</description></item>
/// </list>
/// </summary>
public sealed class DefaultControlTriggerRouter : IControlTriggerRouter
{
    private static readonly WorkflowDefinition AlertCreatedWorkflow = new(
        WorkflowKey: "alert-created-default",
        Description: "Default execution for new alert control triggers.",
        Steps:
        [
            new WorkflowStepDefinition("EvaluateTrigger", "Internal", 1),
            new WorkflowStepDefinition("InitializeWorkflowContext", "Internal", 2),
            new WorkflowStepDefinition("RecordExecution", "Internal", 3)
        ]);

    private static readonly WorkflowDefinition AlertReopenedWorkflow = new(
        WorkflowKey: "alert-reopened-default",
        Description: "Default execution for reopened alert control triggers.",
        Steps:
        [
            new WorkflowStepDefinition("EvaluateTrigger", "Internal", 1),
            new WorkflowStepDefinition("EscalationCheck", "Internal", 2),
            new WorkflowStepDefinition("RecordExecution", "Internal", 3)
        ]);

    private static readonly WorkflowDefinition AlertCreatedEscalatedWorkflow = new(
        WorkflowKey: "alert-created-escalated",
        Description: "Escalated variant when advisory selects context_escalated.",
        Steps:
        [
            new WorkflowStepDefinition("EvaluateTrigger", "Internal", 1),
            new WorkflowStepDefinition("EscalationBranch", "Internal", 2),
            new WorkflowStepDefinition("RecordExecution", "Internal", 3)
        ]);

    private static readonly WorkflowDefinition AlertCreatedMemoryInformedWorkflow = new(
        WorkflowKey: "alert-created-memory-informed",
        Description: "Memory-informed variant when advisory selects memory_informed.",
        Steps:
        [
            new WorkflowStepDefinition("EvaluateTrigger", "Internal", 1),
            new WorkflowStepDefinition("MemoryContextMerge", "Internal", 2),
            new WorkflowStepDefinition("RecordExecution", "Internal", 3)
        ]);

    private static readonly WorkflowDefinition AlertReopenedEscalatedWorkflow = new(
        WorkflowKey: "alert-reopened-escalated",
        Description: "Escalated variant for reopened alerts when advisory selects context_escalated.",
        Steps:
        [
            new WorkflowStepDefinition("EvaluateTrigger", "Internal", 1),
            new WorkflowStepDefinition("EscalationCheck", "Internal", 2),
            new WorkflowStepDefinition("EscalationFollowThrough", "Internal", 3),
            new WorkflowStepDefinition("RecordExecution", "Internal", 4)
        ]);

    public WorkflowDefinition? ResolveWorkflow(
        ReceiveControlTriggerCommand command,
        ControlAdvisoryRouteHint? advisoryHint = null)
    {
        var strategyKey = advisoryHint?.SelectedStrategyKey?.Trim();
        return command.LifecycleEventType switch
        {
            "AlertCreated" => MapAlertCreated(strategyKey),
            "AlertReopened" => MapAlertReopened(strategyKey),
            _ => null
        };
    }

    private static WorkflowDefinition MapAlertCreated(string? strategyKey)
    {
        if (string.Equals(strategyKey, AdvisoryStrategyKeys.ContextEscalated, StringComparison.Ordinal))
            return AlertCreatedEscalatedWorkflow;
        if (string.Equals(strategyKey, AdvisoryStrategyKeys.MemoryInformed, StringComparison.Ordinal))
            return AlertCreatedMemoryInformedWorkflow;
        return AlertCreatedWorkflow;
    }

    private static WorkflowDefinition MapAlertReopened(string? strategyKey)
    {
        if (string.Equals(strategyKey, AdvisoryStrategyKeys.ContextEscalated, StringComparison.Ordinal))
            return AlertReopenedEscalatedWorkflow;
        return AlertReopenedWorkflow;
    }
}
