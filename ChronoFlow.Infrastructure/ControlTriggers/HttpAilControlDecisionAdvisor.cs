using System.Net.Http.Json;
using System.Text.Json;
using ChronoFlow.Modules.ControlTriggers.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChronoFlow.Infrastructure.ControlTriggers;

/// <summary>Calls AIL <c>POST /decisions</c>; failures degrade to null (local routing fallback).</summary>
public sealed class HttpAilControlDecisionAdvisor : IControlDecisionAdvisor
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _http;
    private readonly IOptions<ControlTriggersAdvisoryOptions> _options;
    private readonly ILogger<HttpAilControlDecisionAdvisor> _logger;

    public HttpAilControlDecisionAdvisor(
        HttpClient http,
        IOptions<ControlTriggersAdvisoryOptions> options,
        ILogger<HttpAilControlDecisionAdvisor> logger)
    {
        _http = http;
        _options = options;
        _logger = logger;
    }

    public async Task<ControlAdvisoryOutcome?> GetAdvisoryAsync(
        ReceiveControlTriggerCommand command,
        CancellationToken cancellationToken = default)
    {
        var opts = _options.Value;
        if (!opts.Enabled)
            return null;

        if (string.IsNullOrWhiteSpace(opts.BaseUrl))
        {
            _logger.LogWarning("Control trigger advisory is enabled but ControlTriggers:Advisory:BaseUrl is missing; skipping AIL consult.");
            return null;
        }

        try
        {
            var payload = BuildAilRequest(command, opts);
            using var response = await _http
                .PostAsJsonAsync("decisions", payload, SerializerOptions, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "AIL decisions endpoint returned {StatusCode}; using default local routing.",
                    (int)response.StatusCode);
                return null;
            }

            var dto = await response.Content
                .ReadFromJsonAsync<AilDecideResponseDto>(SerializerOptions, cancellationToken)
                .ConfigureAwait(false);

            if (dto is null || string.IsNullOrWhiteSpace(dto.SelectedStrategyKey))
                return null;

            return new ControlAdvisoryOutcome(
                dto.SelectedStrategyKey.Trim(),
                string.IsNullOrWhiteSpace(dto.Confidence) ? "Low" : dto.Confidence.Trim(),
                dto.ReasonSummary ?? "",
                dto.UsedMemory,
                dto.MemoryItemCount);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("AIL advisory request timed out; using default local routing.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AIL advisory request failed; using default local routing.");
            return null;
        }
    }

    private static object BuildAilRequest(ReceiveControlTriggerCommand command, ControlTriggersAdvisoryOptions opts)
    {
        var structured = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["trigger_type"] = command.TriggerType,
            ["lifecycle_event_type"] = command.LifecycleEventType,
            ["current_status"] = command.CurrentStatus,
            ["rule_id"] = command.RuleId.ToString("D"),
            ["signal_id"] = command.SignalId.ToString("D"),
            ["has_been_reopened"] = command.HasBeenReopened ? "true" : "false"
        };

        if (!string.IsNullOrWhiteSpace(command.RuleName))
            structured["rule_name"] = command.RuleName!;

        return new
        {
            tenantId = opts.DefaultTenantId,
            decisionType = opts.DecisionType,
            subjectType = "control_trigger",
            subjectId = command.AlertId.ToString("D"),
            contextText = (string?)null,
            structuredContext = structured,
            includeMemory = false,
            memoryQuery = (object?)null,
            candidateStrategies = (IReadOnlyList<string>?)null,
            metadata = (IReadOnlyDictionary<string, string>?)null
        };
    }

    private sealed class AilDecideResponseDto
    {
        public string? SelectedStrategyKey { get; set; }
        public string? Confidence { get; set; }
        public string? ReasonSummary { get; set; }
        public bool UsedMemory { get; set; }
        public int MemoryItemCount { get; set; }
    }
}
