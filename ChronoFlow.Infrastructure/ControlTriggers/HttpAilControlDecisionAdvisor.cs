using ChronoFlow.Modules.ControlTriggers.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChronoFlow.Infrastructure.ControlTriggers;

/// <summary>Maps control-trigger commands to <see cref="AdvisoryExecutionRequest"/> and delegates to <see cref="IAilExecutionClient"/>.</summary>
public sealed class HttpAilControlDecisionAdvisor : IControlDecisionAdvisor
{
    private readonly IAilExecutionClient _ailClient;
    private readonly IOptions<ControlTriggersAdvisoryOptions> _options;
    private readonly ILogger<HttpAilControlDecisionAdvisor> _logger;

    public HttpAilControlDecisionAdvisor(
        IAilExecutionClient ailClient,
        IOptions<ControlTriggersAdvisoryOptions> options,
        ILogger<HttpAilControlDecisionAdvisor> logger)
    {
        _ailClient = ailClient;
        _options = options;
        _logger = logger;
    }

    public async Task<AdvisoryExecutionResult> GetAdvisoryAsync(
        ReceiveControlTriggerCommand command,
        Guid orchestrationExecutionInstanceId,
        CancellationToken cancellationToken = default)
    {
        var opts = _options.Value;
        if (!opts.Enabled)
            return new AdvisoryExecutionResult.SkippedNotRequested();

        if (string.IsNullOrWhiteSpace(opts.BaseUrl))
        {
            _logger.LogWarning(
                "Control trigger advisory is enabled but ControlTriggers:Advisory:BaseUrl is missing; advisory unavailable.");
            return new AdvisoryExecutionResult.Unavailable("ail_base_url_missing");
        }

        var request = new AdvisoryExecutionRequest(
            command.TriggerType,
            command.AlertId,
            command.RuleId,
            command.SignalId,
            command.CurrentStatus,
            command.LifecycleEventType,
            command.RuleName,
            command.HasBeenReopened,
            command.CorrelationId,
            orchestrationExecutionInstanceId);

        return await _ailClient
            .RequestAdvisoryAsync(request, cancellationToken)
            .ConfigureAwait(false);
    }
}
