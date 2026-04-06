using ChronoFlow.Modules.ControlTriggers.Application;
using Xunit;

namespace ChronoFlow.Tests.Application;

public sealed class DefaultWorkflowExecutionPolicyTests
{
    private static WorkflowPolicyInput InputWithInboundReason(string? inboundReason) =>
        WorkflowPolicyInput.From(
            new ReceiveControlTriggerCommand(
                "AlertCreated",
                Guid.Parse("a1000000-0000-4000-8000-000000000001"),
                Guid.Parse("b2000000-0000-4000-8000-000000000002"),
                Guid.Parse("c3000000-0000-4000-8000-000000000003"),
                DateTimeOffset.UtcNow,
                "Open",
                "AlertCreated",
                null,
                null,
                null,
                "R",
                false),
            AdvisoryExecutionSnapshot.Empty with { InboundDecisionReasonCode = inboundReason },
            "alert-created-default");

    [Fact]
    public void Same_input_yields_identical_decision()
    {
        var sut = new DefaultWorkflowExecutionPolicy();
        var input = InputWithInboundReason(OrchestrationPolicyInboundReasonCodes.SuppressExecution);
        var a = sut.Evaluate(input);
        var b = sut.Evaluate(input);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Empty_inbound_reason_proceeds()
    {
        var sut = new DefaultWorkflowExecutionPolicy();
        var d = sut.Evaluate(InputWithInboundReason(null));
        Assert.Equal(WorkflowPolicyKind.Proceed, d.Kind);
    }

    [Fact]
    public void Unknown_inbound_reason_proceeds()
    {
        var sut = new DefaultWorkflowExecutionPolicy();
        var d = sut.Evaluate(InputWithInboundReason("tenant_custom_rule"));
        Assert.Equal(WorkflowPolicyKind.Proceed, d.Kind);
    }

    [Fact]
    public void Evaluation_does_not_mutate_input()
    {
        var sut = new DefaultWorkflowExecutionPolicy();
        var input = InputWithInboundReason(OrchestrationPolicyInboundReasonCodes.RequireReview);
        var before = input.InboundDecisionReasonCode;
        _ = sut.Evaluate(input);
        Assert.Equal(before, input.InboundDecisionReasonCode);
    }
}
