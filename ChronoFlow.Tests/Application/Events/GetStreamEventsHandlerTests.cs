using ChronoFlow.Modules.Events.Application;
using ChronoFlow.Modules.Events.Domain;
using Xunit;

namespace ChronoFlow.Tests.Application.Events;

public sealed class GetStreamEventsHandlerTests
{
    [Fact]
    public async Task HandleAsync_Returns_Only_Events_For_Requested_Stream_In_Order()
    {
        var repository = new InMemoryEventRepository();

        await repository.AddAsync(EventRecord.Create(
            "order-123",
            "order.confirmed",
            "{\"confirmedBy\":\"system\"}",
            new DateTime(2026, 3, 10, 23, 57, 0, DateTimeKind.Utc),
            Guid.NewGuid()), CancellationToken.None);

        await repository.AddAsync(EventRecord.Create(
            "order-123",
            "order.created",
            "{\"amount\":42}",
            new DateTime(2026, 3, 10, 23, 56, 0, DateTimeKind.Utc),
            Guid.NewGuid()), CancellationToken.None);

        await repository.AddAsync(EventRecord.Create(
            "order-999",
            "other.stream.event",
            "{}",
            new DateTime(2026, 3, 10, 23, 55, 0, DateTimeKind.Utc),
            Guid.NewGuid()), CancellationToken.None);

        var handler = new GetStreamEventsHandler(repository);

        var result = await handler.HandleAsync(
            new GetStreamEventsQuery("order-123"),
            CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("order-123", result[0].StreamId);
        Assert.Equal("order.created", result[0].EventType);
        Assert.Equal("order.confirmed", result[1].EventType);
        Assert.All(result, x => Assert.Equal("order-123", x.StreamId));
    }

    [Fact]
    public async Task HandleAsync_Returns_Empty_List_When_Stream_Does_Not_Exist()
    {
        var repository = new InMemoryEventRepository();
        var handler = new GetStreamEventsHandler(repository);

        var result = await handler.HandleAsync(
            new GetStreamEventsQuery("missing-stream"),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result);
    }
}