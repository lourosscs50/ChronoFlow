namespace ChronoFlow.Modules.ControlTriggers.Application;

/// <summary>
/// Default platform policy: proceeds unless inbound reason code matches well-known orchestration gates.
/// </summary>
public sealed class DefaultWorkflowExecutionPolicy : IWorkflowExecutionPolicy
{
    public WorkflowExecutionPolicyDecision Evaluate(WorkflowPolicyInput input)
    {
        var code = input.InboundDecisionReasonCode;
        if (string.IsNullOrEmpty(code))
            return new WorkflowExecutionPolicyDecision(WorkflowPolicyKind.Proceed);

        if (string.Equals(code, OrchestrationPolicyInboundReasonCodes.SuppressExecution, StringComparison.Ordinal))
            return new WorkflowExecutionPolicyDecision(WorkflowPolicyKind.Suppress);

        if (string.Equals(code, OrchestrationPolicyInboundReasonCodes.AdvisoryOnly, StringComparison.Ordinal))
            return new WorkflowExecutionPolicyDecision(WorkflowPolicyKind.AdvisoryOnly);

        if (string.Equals(code, OrchestrationPolicyInboundReasonCodes.RequireReview, StringComparison.Ordinal))
            return new WorkflowExecutionPolicyDecision(WorkflowPolicyKind.RequireReview);

        return new WorkflowExecutionPolicyDecision(WorkflowPolicyKind.Proceed);
    }
}
