using System.Reflection;
using ChronoFlow.Modules.ControlTriggers.Application;
using Xunit;

namespace ChronoFlow.Tests.Application;

public sealed class ControlTriggersApplicationArchitectureTests
{
    [Fact]
    public void ControlTriggers_application_assembly_does_not_reference_AIL()
    {
        var asm = typeof(IControlDecisionAdvisor).Assembly;
        var bad = asm.GetReferencedAssemblies()
            .Where(a => a.Name?.StartsWith("AIL.", StringComparison.Ordinal) == true)
            .Select(a => a.Name)
            .ToList();

        Assert.Empty(bad);
    }

    [Fact]
    public void ControlTriggers_application_assembly_does_not_reference_ChronoFlow_Infrastructure()
    {
        var asm = typeof(IControlDecisionAdvisor).Assembly;
        var bad = asm.GetReferencedAssemblies()
            .Where(a => string.Equals(a.Name, "ChronoFlow.Infrastructure", StringComparison.Ordinal))
            .Select(a => a.Name)
            .ToList();

        Assert.Empty(bad);
    }
}
