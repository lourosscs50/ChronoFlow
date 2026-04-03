namespace ChronoFlow.Modules.ControlTriggers.Application;

/// <summary>Bounded hint derived from advisory; ChronoFlow maps this to local workflow keys deterministically.</summary>
public sealed record ControlAdvisoryRouteHint(string SelectedStrategyKey);
