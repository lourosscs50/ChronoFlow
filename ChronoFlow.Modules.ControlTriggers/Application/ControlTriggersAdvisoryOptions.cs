namespace ChronoFlow.Modules.ControlTriggers.Application;

/// <summary>Configuration for optional AIL advisory calls before local workflow routing (bounded integration).</summary>
public sealed class ControlTriggersAdvisoryOptions
{
    public const string SectionName = "ControlTriggers:Advisory";

    /// <summary>When false, no outbound advisory call is made (default).</summary>
    public bool Enabled { get; set; }

    /// <summary>Base URL of the AIL HTTP API (e.g. https://ail-host/). Trailing slash optional.</summary>
    public string? BaseUrl { get; set; }

    /// <summary>Optional service-to-service key sent as X-Api-Key when non-empty.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Tenant identifier forwarded to AIL decision requests (no ChronoFlow workflow identifiers).</summary>
    public Guid DefaultTenantId { get; set; } = Guid.Parse("00000000-0000-4000-8000-000000000001");

    public int TimeoutSeconds { get; set; } = 5;

    /// <summary>DecisionType value sent to AIL for control-trigger routing advisory.</summary>
    public string DecisionType { get; set; } = "control_trigger_routing";
}
