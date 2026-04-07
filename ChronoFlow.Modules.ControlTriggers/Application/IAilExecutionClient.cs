namespace ChronoFlow.Modules.ControlTriggers.Application;

/// <summary>Infrastructure port: invokes A.I.L. (or compatible) advisory using operator-safe contracts only.</summary>
public interface IAilExecutionClient
{
    /// <summary>
    /// Performs the HTTP advisory request. Does not interpret ChronoFlow routing; returns explicit transport/mapping outcomes.
    /// Must not throw for typical HTTP or deserialization failures; surfaces them as <see cref="AdvisoryExecutionResult.Unavailable"/> or <see cref="AdvisoryExecutionResult.Failed"/>.
    /// </summary>
    Task<AdvisoryExecutionResult> RequestAdvisoryAsync(
        AdvisoryExecutionRequest request,
        CancellationToken cancellationToken = default);
}
