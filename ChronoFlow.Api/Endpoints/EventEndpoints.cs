using System.Security.Claims;
using ChronoFlow.Api.Contracts.Events;
using ChronoFlow.Modules.Events.Application;
using Microsoft.AspNetCore.Authorization;

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

        return app;
    }

    [Authorize]
    private static async Task<IResult> IngestEventAsync(
        IngestEventRequest request,
        ClaimsPrincipal user,
        IngestEventHandler handler,
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
}