using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ChronoFlow.Tests.Api;

public sealed class EventsEndpointsTests : IClassFixture<ChronoFlowApiIntegrationTestFactory>
{
    private readonly ChronoFlowApiIntegrationTestFactory _factory;

    public EventsEndpointsTests(ChronoFlowApiIntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_Stream_Events_Without_Token_Returns_Unauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/streams/order-123/events");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Ingest_Two_Events_Then_Get_Stream_Returns_Both_In_Order()
    {
        var client = _factory.CreateClient();

        var email = $"events-{Guid.NewGuid():N}@chronoflow.dev";
        const string password = "Password123!";

        var registerResponse = await client.PostAsJsonAsync("/auth/register", new
        {
            email,
            password
        });

        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await client.PostAsJsonAsync("/auth/login", new
        {
            email,
            password
        });

        loginResponse.EnsureSuccessStatusCode();

        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginPayload);
        Assert.False(string.IsNullOrWhiteSpace(loginPayload!.Token));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginPayload.Token);

        var streamId = $"order-{Guid.NewGuid():N}";
        var firstOccurredAt = new DateTime(2026, 3, 10, 23, 56, 0, DateTimeKind.Utc);
        var secondOccurredAt = new DateTime(2026, 3, 10, 23, 57, 0, DateTimeKind.Utc);

        var ingestFirstResponse = await client.PostAsJsonAsync("/events/", new
{
    streamId,
    eventType = "order.created",
    payload = "{\"amount\":42}",
    occurredAtUtc = firstOccurredAt
});
        ingestFirstResponse.EnsureSuccessStatusCode();

        var ingestSecondResponse = await client.PostAsJsonAsync("/events/", new
        {
        streamId,
        eventType = "order.confirmed",
        payload = "{\"confirmedBy\":\"system\"}",
        occurredAtUtc = secondOccurredAt
        });

        ingestSecondResponse.EnsureSuccessStatusCode();

        var streamResponse = await client.GetAsync($"/streams/{streamId}/events");

        streamResponse.EnsureSuccessStatusCode();

        var events = await streamResponse.Content.ReadFromJsonAsync<List<GetStreamEventItemResponse>>();
        Assert.NotNull(events);
        Assert.Equal(2, events!.Count);

        Assert.Equal("order.created", events[0].EventType);
        Assert.Equal("order.confirmed", events[1].EventType);

        Assert.Equal(firstOccurredAt, events[0].OccurredAtUtc);
        Assert.Equal(secondOccurredAt, events[1].OccurredAtUtc);
    }

    [Fact]
    public async Task Get_Stream_Returns_Empty_List_When_Stream_Has_No_Events()
    {
        var client = _factory.CreateClient();

        var email = $"events-empty-{Guid.NewGuid():N}@chronoflow.dev";
        const string password = "Password123!";

        var registerResponse = await client.PostAsJsonAsync("/auth/register", new
        {
            email,
            password
        });

        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await client.PostAsJsonAsync("/auth/login", new
        {
            email,
            password
        });

        loginResponse.EnsureSuccessStatusCode();

        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginPayload);
        Assert.False(string.IsNullOrWhiteSpace(loginPayload!.Token));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginPayload.Token);

        var missingStreamId = $"missing-{Guid.NewGuid():N}";
         var streamResponse = await client.GetAsync($"/streams/{missingStreamId}/events");

        streamResponse.EnsureSuccessStatusCode();

        var events = await streamResponse.Content.ReadFromJsonAsync<List<GetStreamEventItemResponse>>();
        Assert.NotNull(events);
        Assert.Empty(events!);
    }

    private sealed record LoginResponse(string Token);

    private sealed record GetStreamEventItemResponse(
        Guid EventId,
        string StreamId,
        string EventType,
        string Payload,
        DateTime OccurredAtUtc,
        Guid CreatedByUserId);
}