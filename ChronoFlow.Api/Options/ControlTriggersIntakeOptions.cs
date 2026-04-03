namespace ChronoFlow.Api.Options;

public sealed class ControlTriggersIntakeOptions
{
    public const string SectionName = "ControlTriggers:Intake";

    /// <summary>When set, requests must include a matching X-Api-Key header.</summary>
    public string? ExpectedApiKey { get; set; }
}
