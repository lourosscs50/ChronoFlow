using ChronoFlow.Modules.ControlTriggers.Domain;

namespace ChronoFlow.Modules.ControlTriggers.Application;

/// <summary>Maps a validated control trigger to an in-memory workflow definition, or none when no execution applies.</summary>
public interface IControlTriggerRouter
{
    WorkflowDefinition? ResolveWorkflow(
        ReceiveControlTriggerCommand command,
        ControlAdvisoryRouteHint? advisoryHint = null);
}
