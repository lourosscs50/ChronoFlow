using ChronoFlow.Api.Contracts.Control;
using ChronoFlow.Api.Mapping;
using ChronoFlow.Api.Options;
using ChronoFlow.Api.Security;
using ChronoFlow.Modules.ControlTriggers.Application;
using Microsoft.AspNetCore.Http;
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

        app.MapPost("/control/executions/{id:guid}/approve", ApprovePendingReviewAsync)
            .WithName("ApprovePendingControlExecution")
            .WithTags("Control")
            .AllowAnonymous();

        app.MapPost("/control/executions/{id:guid}/cancel", CancelPendingReviewAsync)
            .WithName("CancelPendingControlExecution")
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
        return Results.Ok(rows.Select(ControlExecutionRecordResponseMapper.ToResponse).ToList());
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

        return Results.Ok(ControlExecutionRecordResponseMapper.ToResponse(row));
    }

    private static async Task<IResult> ApprovePendingReviewAsync(
        Guid id,
        HttpRequest httpRequest,
        [FromServices] IOptions<ControlTriggersIntakeOptions> intakeOptions,
        [FromServices] ApprovePendingControlExecution.Handler handler,
        OperatorReviewActionRequest? body,
        CancellationToken cancellationToken)
    {
        if (!ControlTriggerIntakeApiKey.TryValidate(httpRequest, intakeOptions.Value, out var authError))
            return authError;

        var result = await handler.HandleAsync(id, body?.Note, cancellationToken);
        return MapReviewActionResult(result);
    }

    private static async Task<IResult> CancelPendingReviewAsync(
        Guid id,
        HttpRequest httpRequest,
        [FromServices] IOptions<ControlTriggersIntakeOptions> intakeOptions,
        [FromServices] CancelPendingControlExecution.Handler handler,
        OperatorReviewActionRequest? body,
        CancellationToken cancellationToken)
    {
        if (!ControlTriggerIntakeApiKey.TryValidate(httpRequest, intakeOptions.Value, out var authError))
            return authError;

        var result = await handler.HandleAsync(id, body?.Note, cancellationToken);
        return MapReviewActionResult(result);
    }

    private static IResult MapReviewActionResult(PendingReviewActionResult result)
    {
        if (result.Succeeded && result.Record is not null)
            return Results.Ok(ControlExecutionRecordResponseMapper.ToResponse(result.Record));

        return result.Failure switch
        {
            PendingReviewActionFailureKind.NotFound =>
                Results.NotFound(new { error = "Execution record not found." }),
            PendingReviewActionFailureKind.NotPendingReview =>
                Results.Conflict(new { error = "Execution is not pending operator review." }),
            PendingReviewActionFailureKind.AlreadyFinalized =>
                Results.Conflict(new { error = "A review action was already recorded for this execution." }),
            PendingReviewActionFailureKind.InvalidRecordState =>
                Results.Conflict(new { error = "Execution record is not in a valid state for this action." }),
            PendingReviewActionFailureKind.WorkflowResolutionMismatch =>
                Results.Conflict(new { error = "Stored workflow no longer resolves consistently for this record." }),
            _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
        };
    }
}
