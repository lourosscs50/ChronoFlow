using ChronoFlow.Modules.ControlTriggers.Domain;

namespace ChronoFlow.Modules.ControlTriggers.Application;

/// <summary>Rebuilds intake command from a durable execution row for honest orchestration replay (bounded snapshot fields only).</summary>
public static class ControlExecutionRecordTriggerRebuilder
{
    public static ReceiveControlTriggerCommand ToCommand(ControlExecutionRecord record)
    {
        var occurredAt = record.OccurredAtUtc ?? record.ReceivedAtUtc;
        return new ReceiveControlTriggerCommand(
            record.TriggerType,
            record.AlertId,
            record.RuleId,
            record.SignalId,
            occurredAt,
            record.CurrentStatus,
            record.LifecycleEventType,
            record.AcknowledgedByUserId,
            record.ResolvedByUserId,
            record.ReopenedByUserId,
            record.RuleName,
            record.HasBeenReopened,
            record.CorrelationId,
            ToInboundDecision(record));
    }

    private static InboundDecisionContext? ToInboundDecision(ControlExecutionRecord record)
    {
        if (!HasInboundDecisionSnapshot(record))
            return null;

        return new InboundDecisionContext(
            record.InboundDecisionSummary,
            record.InboundDecisionReferenceId,
            record.InboundDecisionConfidence,
            record.InboundDecisionReasonCode,
            record.InboundLinkedExternalExecutionId);
    }

    private static bool HasInboundDecisionSnapshot(ControlExecutionRecord record) =>
        record.InboundDecisionSummary is not null
        || record.InboundDecisionReferenceId is not null
        || record.InboundDecisionConfidence is not null
        || record.InboundDecisionReasonCode is not null
        || record.InboundLinkedExternalExecutionId is not null;
}
