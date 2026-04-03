using ChronoFlow.Modules.ControlTriggers.Application;

namespace ChronoFlow.Tests.Application;

public sealed class FakeControlDecisionAdvisor(ControlAdvisoryOutcome? outcome) : IControlDecisionAdvisor
{
    public int CallCount { get; private set; }

    public Task<ControlAdvisoryOutcome?> GetAdvisoryAsync(
        ReceiveControlTriggerCommand command,
        CancellationToken cancellationToken = default)
    {
        _ = command;
        _ = cancellationToken;
        CallCount++;
        return Task.FromResult(outcome);
    }
}
