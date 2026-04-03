namespace ChronoFlow.Modules.ControlTriggers.Application;

/// <summary>Application port: obtains an advisory decision for a validated control trigger (AIL or other provider).</summary>
public interface IControlDecisionAdvisor
{
    /// <summary>
    /// Returns null when advisory is disabled, the trigger is ineligible, the provider fails, or the response cannot be mapped.
    /// Implementations must not throw for transport or mapping failures.
    /// </summary>
    Task<ControlAdvisoryOutcome?> GetAdvisoryAsync(
        ReceiveControlTriggerCommand command,
        CancellationToken cancellationToken = default);
}
