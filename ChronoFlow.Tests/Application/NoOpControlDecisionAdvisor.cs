using ChronoFlow.Modules.ControlTriggers.Application;

namespace ChronoFlow.Tests.Application;

/// <summary>Test double: advisory disabled (no HTTP, no side effects).</summary>
public sealed class NoOpControlDecisionAdvisor : IControlDecisionAdvisor
{
    public Task<AdvisoryExecutionResult> GetAdvisoryAsync(
        ReceiveControlTriggerCommand command,
        Guid orchestrationExecutionInstanceId,
        CancellationToken cancellationToken = default)
    {
        _ = command;
        _ = orchestrationExecutionInstanceId;
        _ = cancellationToken;
        return Task.FromResult<AdvisoryExecutionResult>(new AdvisoryExecutionResult.SkippedNotRequested());
    }
}
