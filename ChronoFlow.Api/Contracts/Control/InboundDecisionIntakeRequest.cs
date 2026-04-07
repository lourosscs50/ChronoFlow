using System.Text.Json.Serialization;

namespace ChronoFlow.Api.Contracts.Control;

/// <summary>Optional bounded decision context from upstream intake (operator-safe).</summary>
public sealed record InboundDecisionIntakeRequest(
    [property: JsonPropertyName("summary")] string? Summary,
    [property: JsonPropertyName("referenceId")] string? ReferenceId,
    [property: JsonPropertyName("confidence")] string? Confidence,
    [property: JsonPropertyName("reasonCode")] string? ReasonCode,
    [property: JsonPropertyName("linkedExternalExecutionId")] string? LinkedExternalExecutionId);
