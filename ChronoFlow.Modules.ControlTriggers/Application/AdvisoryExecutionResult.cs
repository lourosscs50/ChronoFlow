namespace ChronoFlow.Modules.ControlTriggers.Application;

/// <summary>Explicit outcome of an advisory invocation (operator-safe reason codes only).</summary>
public abstract record AdvisoryExecutionResult
{
    /// <summary>Advisory was not consulted (disabled or ineligible at the orchestration boundary).</summary>
    public sealed record SkippedNotRequested : AdvisoryExecutionResult;

    /// <summary>Dependency could not provide advisory (unreachable, misconfigured upstream, timeout).</summary>
    public sealed record Unavailable(string ReasonCode) : AdvisoryExecutionResult;

    /// <summary>Advisory was attempted but did not yield a usable outcome (HTTP client error, invalid payload).</summary>
    public sealed record Failed(string ReasonCode) : AdvisoryExecutionResult;

    /// <summary>Advisory completed and returned a normalized routing outcome.</summary>
    public sealed record Succeeded(ControlAdvisoryOutcome Outcome) : AdvisoryExecutionResult;
}
