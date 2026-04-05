using ChronoFlow.Modules.ControlTriggers.Domain;

namespace ChronoFlow.Modules.ControlTriggers.Application;

internal static class ControlExecutionRecordFactory
{
    public static ControlExecutionRecord CreateSuppressed(
        ReceiveControlTriggerCommand command,
        string suppressionReason,
        DateTimeOffset receivedAtUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            ExecutionInstanceId = null,
            TriggerType = command.TriggerType,
            LifecycleEventType = command.LifecycleEventType,
            AlertId = command.AlertId,
            RuleId = command.RuleId,
            SignalId = command.SignalId,
            WorkflowKey = null,
            WasExecuted = false,
            WasSuppressed = true,
            SuppressionReason = suppressionReason,
            ExecutedStepCount = 0,
            ReceivedAtUtc = receivedAtUtc,
            ExecutedAtUtc = null,
            CurrentStatus = command.CurrentStatus,
            AcknowledgedByUserId = command.AcknowledgedByUserId,
            ResolvedByUserId = command.ResolvedByUserId,
            ReopenedByUserId = command.ReopenedByUserId,
            RuleName = command.RuleName,
            HasBeenReopened = command.HasBeenReopened,
            AdvisoryWasUsed = false,
            AdvisoryStrategyKey = null,
            AdvisoryConfidence = null,
            AdvisoryReasonSummary = null
        };

    public static ControlExecutionRecord CreateNoWorkflow(
        ReceiveControlTriggerCommand command,
        DateTimeOffset receivedAtUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            ExecutionInstanceId = null,
            TriggerType = command.TriggerType,
            LifecycleEventType = command.LifecycleEventType,
            AlertId = command.AlertId,
            RuleId = command.RuleId,
            SignalId = command.SignalId,
            WorkflowKey = null,
            WasExecuted = false,
            WasSuppressed = false,
            SuppressionReason = null,
            ExecutedStepCount = 0,
            ReceivedAtUtc = receivedAtUtc,
            ExecutedAtUtc = null,
            CurrentStatus = command.CurrentStatus,
            AcknowledgedByUserId = command.AcknowledgedByUserId,
            ResolvedByUserId = command.ResolvedByUserId,
            ReopenedByUserId = command.ReopenedByUserId,
            RuleName = command.RuleName,
            HasBeenReopened = command.HasBeenReopened,
            AdvisoryWasUsed = false,
            AdvisoryStrategyKey = null,
            AdvisoryConfidence = null,
            AdvisoryReasonSummary = null
        };

    public static ControlExecutionRecord CreateExecuted(
        ReceiveControlTriggerCommand command,
        string workflowKey,
        Guid executionInstanceId,
        int executedStepCount,
        DateTimeOffset receivedAtUtc,
        DateTimeOffset executedAtUtc,
        AdvisoryExecutionSnapshot? advisory = null)
    {
        advisory ??= new AdvisoryExecutionSnapshot(false, null, null, null);
        return new()
        {
            Id = Guid.NewGuid(),
            ExecutionInstanceId = executionInstanceId,
            TriggerType = command.TriggerType,
            LifecycleEventType = command.LifecycleEventType,
            AlertId = command.AlertId,
            RuleId = command.RuleId,
            SignalId = command.SignalId,
            WorkflowKey = workflowKey,
            WasExecuted = true,
            WasSuppressed = false,
            SuppressionReason = null,
            ExecutedStepCount = executedStepCount,
            ReceivedAtUtc = receivedAtUtc,
            ExecutedAtUtc = executedAtUtc,
            CurrentStatus = command.CurrentStatus,
            AcknowledgedByUserId = command.AcknowledgedByUserId,
            ResolvedByUserId = command.ResolvedByUserId,
            ReopenedByUserId = command.ReopenedByUserId,
            RuleName = command.RuleName,
            HasBeenReopened = command.HasBeenReopened,
            AdvisoryWasUsed = advisory.AdvisoryWasUsed,
            AdvisoryStrategyKey = advisory.AdvisoryStrategyKey,
            AdvisoryConfidence = advisory.AdvisoryConfidence,
            AdvisoryReasonSummary = advisory.AdvisoryReasonSummary
        };
    }
}
