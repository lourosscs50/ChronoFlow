using ChronoFlow.Modules.ControlTriggers.Application;
using Microsoft.Extensions.Options;

namespace ChronoFlow.Tests.Application;

internal static class ControlTriggerHandlerTestDefaults
{
    public static IOptions<ControlTriggersAdvisoryOptions> DisabledAdvisoryOptions { get; } =
        Options.Create(new ControlTriggersAdvisoryOptions { Enabled = false });
}
