namespace ChronoFlow.Modules.ControlTriggers.Application;

/// <summary>Application port: obtains an advisory decision for a validated control trigger (A.I.L. or other provider).</summary>
public interface IControlDecisionAdvisor
{
    /// <summary>
    /// Returns explicit <see cref="AdvisoryExecutionResult"/> semantics; orchestration chooses fallback routing when advisory is not successful.
    /// Implementations must not throw for transport or mapping failures.
    /// </summary>
    Task<AdvisoryExecutionResult> GetAdvisoryAsync(
        ReceiveControlTriggerCommand command,
        Guid orchestrationExecutionInstanceId,
        CancellationToken cancellationToken = default);
}
