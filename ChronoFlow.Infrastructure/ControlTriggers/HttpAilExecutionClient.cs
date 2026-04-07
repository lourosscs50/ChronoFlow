using System.Net.Http.Json;
using System.Text.Json;
using ChronoFlow.Modules.ControlTriggers.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChronoFlow.Infrastructure.ControlTriggers;

/// <summary>HTTP adapter for A.I.L. <c>POST /decisions</c>; maps transport outcomes to explicit <see cref="AdvisoryExecutionResult"/> values.</summary>
public sealed class HttpAilExecutionClient : IAilExecutionClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _http;
    private readonly IOptions<ControlTriggersAdvisoryOptions> _options;
    private readonly ILogger<HttpAilExecutionClient> _logger;

    public HttpAilExecutionClient(
        HttpClient http,
        IOptions<ControlTriggersAdvisoryOptions> options,
        ILogger<HttpAilExecutionClient> logger)
    {
        _http = http;
        _options = options;
        _logger = logger;
    }

    public async Task<AdvisoryExecutionResult> RequestAdvisoryAsync(
        AdvisoryExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        var opts = _options.Value;
        try
        {
            var payload = BuildAilRequest(request, opts);
            using var response = await _http
                .PostAsJsonAsync("decisions", payload, SerializerOptions, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                if ((int)response.StatusCode >= 500)
                {
                    _logger.LogWarning(
                        "AIL decisions endpoint returned {StatusCode}; advisory unavailable.",
                        (int)response.StatusCode);
                    return new AdvisoryExecutionResult.Unavailable("ail_http_5xx");
                }

                _logger.LogWarning(
                    "AIL decisions endpoint returned {StatusCode}; advisory failed.",
                    (int)response.StatusCode);
                return new AdvisoryExecutionResult.Failed("ail_http_4xx");
            }

            var dto = await response.Content
                .ReadFromJsonAsync<AilDecideResponseDto>(SerializerOptions, cancellationToken)
                .ConfigureAwait(false);

            if (dto is null || string.IsNullOrWhiteSpace(dto.SelectedStrategyKey))
                return new AdvisoryExecutionResult.Failed("ail_response_invalid");

            var outcome = new ControlAdvisoryOutcome(
                dto.SelectedStrategyKey.Trim(),
                string.IsNullOrWhiteSpace(dto.Confidence) ? "Low" : dto.Confidence.Trim(),
                dto.ReasonSummary ?? "",
                dto.UsedMemory,
                dto.MemoryItemCount,
                string.IsNullOrWhiteSpace(dto.ExecutionId) ? null : dto.ExecutionId.Trim());

            return new AdvisoryExecutionResult.Succeeded(outcome);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("AIL advisory request timed out; advisory unavailable.");
            return new AdvisoryExecutionResult.Unavailable("ail_timeout");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AIL advisory request failed; advisory unavailable.");
            return new AdvisoryExecutionResult.Unavailable("ail_transport_error");
        }
    }

    private static object BuildAilRequest(AdvisoryExecutionRequest request, ControlTriggersAdvisoryOptions opts)
    {
        var structured = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["trigger_type"] = request.TriggerType,
            ["lifecycle_event_type"] = request.LifecycleEventType,
            ["current_status"] = request.CurrentStatus,
            ["rule_id"] = request.RuleId.ToString("D"),
            ["signal_id"] = request.SignalId.ToString("D"),
            ["has_been_reopened"] = request.HasBeenReopened ? "true" : "false"
        };

        if (!string.IsNullOrWhiteSpace(request.RuleName))
            structured["rule_name"] = request.RuleName!;

        var metadata = BuildMetadata(request);

        return new
        {
            tenantId = opts.DefaultTenantId,
            decisionType = opts.DecisionType,
            subjectType = "control_trigger",
            subjectId = request.AlertId.ToString("D"),
            contextText = (string?)null,
            structuredContext = structured,
            includeMemory = false,
            memoryQuery = (object?)null,
            candidateStrategies = (IReadOnlyList<string>?)null,
            metadata
        };
    }

    private static IReadOnlyDictionary<string, string>? BuildMetadata(AdvisoryExecutionRequest request)
    {
        var d = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["orchestrationExecutionInstanceId"] = request.OrchestrationExecutionInstanceId.ToString("D")
        };

        if (!string.IsNullOrWhiteSpace(request.CorrelationId))
            d["correlationId"] = request.CorrelationId.Trim();

        return d;
    }

    private sealed class AilDecideResponseDto
    {
        public string? SelectedStrategyKey { get; set; }
        public string? Confidence { get; set; }
        public string? ReasonSummary { get; set; }
        public bool UsedMemory { get; set; }
        public int MemoryItemCount { get; set; }
        public string? ExecutionId { get; set; }
    }
}
