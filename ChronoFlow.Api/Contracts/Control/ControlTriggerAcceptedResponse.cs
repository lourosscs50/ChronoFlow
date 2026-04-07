using System.Text.Json.Serialization;

namespace ChronoFlow.Api.Contracts.Control;

/// <summary>202 payload for successful control trigger intake (execution / suppression summary).</summary>
public sealed record ControlTriggerAcceptedResponse(
    [property: JsonPropertyName("wasExecuted")] bool WasExecuted,
    [property: JsonPropertyName("wasSuppressed")] bool WasSuppressed,
    [property: JsonPropertyName("suppressionReason")] string? SuppressionReason,
    [property: JsonPropertyName("workflowKey")] string? WorkflowKey,
    [property: JsonPropertyName("executedStepCount")] int ExecutedStepCount,
    [property: JsonPropertyName("executionRecordId")] Guid? ExecutionRecordId,
    [property: JsonPropertyName("executionInstanceId")] Guid? ExecutionInstanceId = null,
    [property: JsonPropertyName("pendingOperatorReview")] bool PendingOperatorReview = false,
    [property: JsonPropertyName("orchestrationPolicyOutcome")] string? OrchestrationPolicyOutcome = null);
