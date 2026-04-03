namespace ChronoFlow.Modules.ControlTriggers.Application;

/// <summary>Stable AIL decision strategy keys ChronoFlow maps locally (strings only — no AIL assembly reference).</summary>
public static class AdvisoryStrategyKeys
{
    public const string ContextEscalated = "context_escalated";
    public const string MemoryInformed = "memory_informed";
    public const string DefaultSafe = "default_safe";
    public const string CandidateMatch = "candidate_match";
}
