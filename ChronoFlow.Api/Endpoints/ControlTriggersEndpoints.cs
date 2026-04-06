using ChronoFlow.Api.Contracts.Control;
using ChronoFlow.Api.Options;
using ChronoFlow.Api.Security;
using ChronoFlow.Modules.ControlTriggers.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ChronoFlow.Api.Endpoints;

public static class ControlTriggersEndpoints
{
    public static IEndpointRouteBuilder MapControlTriggersEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/control/triggers", ReceiveControlTriggerAsync)
            .WithName("ReceiveControlTrigger")
            .WithTags("Control")
            .AllowAnonymous();

        return app;
    }

    private static async Task<IResult> ReceiveControlTriggerAsync(
        ReceiveControlTriggerRequest request,
        HttpRequest httpRequest,
        [FromServices] IOptions<ControlTriggersIntakeOptions> intakeOptions,
        [FromServices] ReceiveControlTriggerHandler handler,
        CancellationToken cancellationToken)
    {
        if (!ControlTriggerIntakeApiKey.TryValidate(httpRequest, intakeOptions.Value, out var authError))
            return authError;

        var inbound = request.InboundDecision is null
            ? null
            : new InboundDecisionContext(
                request.InboundDecision.Summary,
                request.InboundDecision.ReferenceId,
                request.InboundDecision.Confidence,
                request.InboundDecision.ReasonCode,
                request.InboundDecision.LinkedExternalExecutionId);

        var command = new ReceiveControlTriggerCommand(
            request.TriggerType,
            request.AlertId,
            request.RuleId,
            request.SignalId,
            request.OccurredAtUtc,
            request.CurrentStatus,
            request.LifecycleEventType,
            request.AcknowledgedByUserId,
            request.ResolvedByUserId,
            request.ReopenedByUserId,
            request.RuleName,
            request.HasBeenReopened,
            request.CorrelationId,
            inbound);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (!result.Accepted)
            return Results.BadRequest(new { error = result.ErrorMessage });

        return Results.Accepted(
            "/control/triggers",
            new ControlTriggerAcceptedResponse(
                result.WasExecuted,
                result.WasSuppressed,
                result.SuppressionReason,
                result.WorkflowKey,
                result.ExecutedStepCount,
                result.ExecutionRecordId,
                result.ExecutionInstanceId,
                result.PendingOperatorReview,
                result.OrchestrationPolicyOutcome));
    }
}
