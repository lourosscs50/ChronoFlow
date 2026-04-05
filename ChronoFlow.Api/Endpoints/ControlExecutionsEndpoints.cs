using ChronoFlow.Api.Contracts.Control;
using ChronoFlow.Api.Options;
using ChronoFlow.Api.Security;
using ChronoFlow.Modules.ControlTriggers.Application;
using ChronoFlow.Modules.ControlTriggers.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ChronoFlow.Api.Endpoints;

public static class ControlExecutionsEndpoints
{
    public static IEndpointRouteBuilder MapControlExecutionsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/control/executions", ListExecutionsAsync)
            .WithName("ListControlExecutions")
            .WithTags("Control")
            .AllowAnonymous();

        app.MapGet("/control/executions/{id:guid}", GetExecutionByIdAsync)
            .WithName("GetControlExecutionById")
            .WithTags("Control")
            .AllowAnonymous();

        return app;
    }

    private static async Task<IResult> ListExecutionsAsync(
        HttpRequest httpRequest,
        [FromServices] IOptions<ControlTriggersIntakeOptions> intakeOptions,
        [FromServices] ListControlExecutions.Handler handler,
        Guid? alertId,
        string? lifecycleEventType,
        bool? wasExecuted,
        bool? wasSuppressed,
        string? workflowKey,
        int skip = 0,
        int? take = null,
        CancellationToken cancellationToken = default)
    {
        if (!ControlTriggerIntakeApiKey.TryValidate(httpRequest, intakeOptions.Value, out var authError))
            return authError;

        var query = new ControlExecutionRecordQuery(
            alertId,
            lifecycleEventType,
            wasExecuted,
            wasSuppressed,
            workflowKey,
            skip,
            take ?? 50);

        var rows = await handler.HandleAsync(query, cancellationToken);
        return Results.Ok(rows.Select(ToResponse).ToList());
    }

    private static async Task<IResult> GetExecutionByIdAsync(
        Guid id,
        HttpRequest httpRequest,
        [FromServices] IOptions<ControlTriggersIntakeOptions> intakeOptions,
        [FromServices] GetControlExecutionById.Handler handler,
        CancellationToken cancellationToken)
    {
        if (!ControlTriggerIntakeApiKey.TryValidate(httpRequest, intakeOptions.Value, out var authError))
            return authError;

        var row = await handler.HandleAsync(id, cancellationToken);
        if (row is null)
            return Results.NotFound(new { error = "Execution record not found." });

        return Results.Ok(ToResponse(row));
    }

    private static ControlExecutionRecordResponse ToResponse(ControlExecutionRecord x) =>
        new(
            x.Id,
            x.TriggerType,
            x.LifecycleEventType,
            x.AlertId,
            x.RuleId,
            x.SignalId,
            x.WorkflowKey,
            x.WasExecuted,
            x.WasSuppressed,
            x.SuppressionReason,
            x.ExecutedStepCount,
            x.ReceivedAtUtc,
            x.ExecutedAtUtc,
            x.CurrentStatus,
            x.AdvisoryWasUsed,
            x.AdvisoryStrategyKey,
            x.AdvisoryConfidence,
            x.AdvisoryReasonSummary,
            x.ExecutionInstanceId);
}
