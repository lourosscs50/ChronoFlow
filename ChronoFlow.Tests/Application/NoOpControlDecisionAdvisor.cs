using ChronoFlow.Modules.ControlTriggers.Application;

namespace ChronoFlow.Tests.Application;

/// <summary>Test double: advisory disabled (no HTTP, no side effects).</summary>
public sealed class NoOpControlDecisionAdvisor : IControlDecisionAdvisor
{
    public Task<ControlAdvisoryOutcome?> GetAdvisoryAsync(
        ReceiveControlTriggerCommand command,
        CancellationToken cancellationToken = default)
    {
        _ = command;
        _ = cancellationToken;
        return Task.FromResult<ControlAdvisoryOutcome?>(null);
    }
}
