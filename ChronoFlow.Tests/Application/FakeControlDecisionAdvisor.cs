using ChronoFlow.Modules.ControlTriggers.Application;

namespace ChronoFlow.Tests.Application;

public sealed class FakeControlDecisionAdvisor : IControlDecisionAdvisor
{
    private readonly AdvisoryExecutionResult _result;

    public FakeControlDecisionAdvisor(ControlAdvisoryOutcome outcome)
        : this(new AdvisoryExecutionResult.Succeeded(outcome))
    {
    }

    public FakeControlDecisionAdvisor(AdvisoryExecutionResult result) => _result = result;

    public int CallCount { get; private set; }

    public Guid? LastOrchestrationExecutionInstanceId { get; private set; }

    public Task<AdvisoryExecutionResult> GetAdvisoryAsync(
        ReceiveControlTriggerCommand command,
        Guid orchestrationExecutionInstanceId,
        CancellationToken cancellationToken = default)
    {
        _ = command;
        _ = cancellationToken;
        CallCount++;
        LastOrchestrationExecutionInstanceId = orchestrationExecutionInstanceId;
        return Task.FromResult(_result);
    }
}
