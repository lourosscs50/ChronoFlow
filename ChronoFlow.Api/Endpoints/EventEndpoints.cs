using System.Security.Claims;
using ChronoFlow.Api.Contracts.Events;
using ChronoFlow.Modules.Events.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChronoFlow.Api.Endpoints;

public static class EventsEndpoints
{
    public static IEndpointRouteBuilder MapEventsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/events")
            .RequireAuthorization();

        group.MapPost("/", IngestEventAsync)
            .WithName("IngestEvent")
            .WithTags("Events");

        group.MapGet("/{id:guid}", GetEventByIdAsync)
            .WithName("GetEventById")
            .WithTags("Events");

        app.MapGet("/streams/{streamId}/events", GetStreamEventsAsync)
            .RequireAuthorization()
            .WithName("GetStreamEvents")
            .WithTags("Events");

        return app;
    }

    [Authorize]
    private static async Task<IResult> IngestEventAsync(
        IngestEventRequest request,
        ClaimsPrincipal user,
        [FromServices] IngestEventHandler handler,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.StreamId))
            return Results.BadRequest(new { error = "StreamId is required." });

        if (string.IsNullOrWhiteSpace(request.EventType))
            return Results.BadRequest(new { error = "EventType is required." });

        if (string.IsNullOrWhiteSpace(request.Payload))
            return Results.BadRequest(new { error = "Payload is required." });

        var userIdValue =
            user.FindFirstValue("uid") ??
            user.FindFirstValue(ClaimTypes.NameIdentifier) ??
            user.FindFirstValue("sub");

        if (!Guid.TryParse(userIdValue, out var createdByUserId) || createdByUserId == Guid.Empty)
            return Results.Unauthorized();

        var command = new IngestEventCommand(
            request.StreamId,
            request.EventType,
            request.Payload,
            request.OccurredAtUtc,
            createdByUserId);

        var result = await handler.HandleAsync(command, cancellationToken);

        return Results.Created($"/events/{result.EventId}", new IngestEventResponse(result.EventId));
    }

    [Authorize]
    private static async Task<IResult> GetEventByIdAsync(
        Guid id,
        [FromServices] GetEventHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetEventQuery(id), cancellationToken);

        if (result is null)
            return Results.NotFound(new { error = "Event not found." });

        return Results.Ok(new GetEventResponse(
            result.EventId,
            result.StreamId,
            result.EventType,
            result.Payload,
            result.OccurredAtUtc,
            result.CreatedByUserId));
    }

    [Authorize]
    private static async Task<IResult> GetStreamEventsAsync(
        string streamId,
        [FromServices] GetStreamEventsHandler handler,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(streamId))
            return Results.BadRequest(new { error = "StreamId is required." });

        var results = await handler.HandleAsync(new GetStreamEventsQuery(streamId), cancellationToken);

        var response = results
            .Select(x => new GetStreamEventItemResponse(
                x.EventId,
                x.StreamId,
                x.EventType,
                x.Payload,
                x.OccurredAtUtc,
                x.CreatedByUserId))
            .ToList();

        return Results.Ok(response);
    }
}