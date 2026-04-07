using System.Text.Json.Serialization;

namespace ChronoFlow.Api.Contracts.Control;

public sealed record OperatorReviewActionRequest(
    [property: JsonPropertyName("note")] string? Note = null);
