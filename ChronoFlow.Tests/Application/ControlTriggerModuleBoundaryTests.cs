using ChronoFlow.Modules.ControlTriggers.Application;
using ChronoFlow.Modules.ControlTriggers.Domain;
using Xunit;

namespace ChronoFlow.Tests.Application;

/// <summary>Ensures execution-layer types remain ChronoFlow-local (no external product assemblies).</summary>
public sealed class ControlTriggerModuleBoundaryTests
{
    [Fact]
    public void Workflow_and_execution_abstractions_live_under_ChronoFlow_modules()
    {
        Assert.StartsWith("ChronoFlow.Modules.ControlTriggers", typeof(WorkflowDefinition).FullName, StringComparison.Ordinal);
        Assert.StartsWith("ChronoFlow.Modules.ControlTriggers", typeof(IWorkflowExecutor).FullName!, StringComparison.Ordinal);
        Assert.StartsWith("ChronoFlow.Modules.ControlTriggers", typeof(IControlTriggerRouter).FullName!, StringComparison.Ordinal);
        Assert.StartsWith("ChronoFlow.Modules.ControlTriggers", typeof(IControlTriggerDeduplicator).FullName!, StringComparison.Ordinal);
        Assert.StartsWith("ChronoFlow.Modules.ControlTriggers", typeof(IProcessedTriggerStore).FullName!, StringComparison.Ordinal);
        Assert.StartsWith("ChronoFlow.Modules.ControlTriggers", typeof(IControlDecisionAdvisor).FullName!, StringComparison.Ordinal);
    }
}
