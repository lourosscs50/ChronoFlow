namespace ChronoFlow.Modules.ControlTriggers.Application;

/// <summary>
/// Pure orchestration policy: decides execution gate from bounded trigger-time input only.
/// </summary>
public interface IWorkflowExecutionPolicy
{
    /// <summary>Must be deterministic and side-effect free; must not mutate <paramref name="input"/>.</summary>
    WorkflowExecutionPolicyDecision Evaluate(WorkflowPolicyInput input);
}
