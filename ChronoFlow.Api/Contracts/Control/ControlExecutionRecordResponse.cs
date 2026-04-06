using System.Text.Json.Serialization;

namespace ChronoFlow.Api.Contracts.Control;

public sealed record ControlExecutionRecordResponse(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("triggerType")] string TriggerType,
    [property: JsonPropertyName("lifecycleEventType")] string LifecycleEventType,
    [property: JsonPropertyName("alertId")] Guid AlertId,
    [property: JsonPropertyName("ruleId")] Guid RuleId,
    [property: JsonPropertyName("signalId")] Guid SignalId,
    [property: JsonPropertyName("workflowKey")] string? WorkflowKey,
    [property: JsonPropertyName("wasExecuted")] bool WasExecuted,
    [property: JsonPropertyName("wasSuppressed")] bool WasSuppressed,
    [property: JsonPropertyName("suppressionReason")] string? SuppressionReason,
    [property: JsonPropertyName("executedStepCount")] int ExecutedStepCount,
    [property: JsonPropertyName("receivedAtUtc")] DateTimeOffset ReceivedAtUtc,
    [property: JsonPropertyName("executedAtUtc")] DateTimeOffset? ExecutedAtUtc,
    [property: JsonPropertyName("currentStatus")] string CurrentStatus,
    [property: JsonPropertyName("advisoryWasUsed")] bool AdvisoryWasUsed,
    [property: JsonPropertyName("advisoryStrategyKey")] string? AdvisoryStrategyKey,
    [property: JsonPropertyName("advisoryConfidence")] string? AdvisoryConfidence,
    [property: JsonPropertyName("advisoryReasonSummary")] string? AdvisoryReasonSummary,
    [property: JsonPropertyName("linkedAilExecutionId")] string? LinkedAilExecutionId,
    [property: JsonPropertyName("inboundDecisionSummary")] string? InboundDecisionSummary,
    [property: JsonPropertyName("inboundDecisionReferenceId")] string? InboundDecisionReferenceId,
    [property: JsonPropertyName("inboundDecisionConfidence")] string? InboundDecisionConfidence,
    [property: JsonPropertyName("inboundDecisionReasonCode")] string? InboundDecisionReasonCode,
    [property: JsonPropertyName("inboundLinkedExternalExecutionId")] string? InboundLinkedExternalExecutionId,
    [property: JsonPropertyName("executionInstanceId")] Guid? ExecutionInstanceId);
