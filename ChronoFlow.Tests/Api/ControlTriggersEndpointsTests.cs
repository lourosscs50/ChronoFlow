using System.Net;
using System.Net.Http.Json;
using ChronoFlow.Api.Contracts.Control;
using Xunit;

namespace ChronoFlow.Tests.Api;

public sealed class ControlTriggersEndpointsTests : IClassFixture<ChronoFlowIntakeTestFactory>
{
    private readonly ChronoFlowIntakeTestFactory _factory;

    public ControlTriggersEndpointsTests(ChronoFlowIntakeTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_control_triggers_returns_401_when_api_key_configured_but_missing()
    {
        var client = _factory.CreateClient();

        var body = ValidBody();
        var response = await client.PostAsJsonAsync("/control/triggers", body);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_control_triggers_accepts_valid_payload_with_api_key()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ChronoFlowIntakeTestFactory.IntakeApiKey);

        var response = await client.PostAsJsonAsync("/control/triggers", ValidBody());

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ControlTriggerAcceptedResponse>();
        Assert.NotNull(payload);
        Assert.True(payload!.WasExecuted);
        Assert.False(payload.WasSuppressed);
        Assert.Null(payload.SuppressionReason);
        Assert.Equal("alert-created-default", payload.WorkflowKey);
        Assert.Equal(3, payload.ExecutedStepCount);
        Assert.NotNull(payload.ExecutionRecordId);
        Assert.NotNull(payload.ExecutionInstanceId);
        Assert.NotEqual(Guid.Empty, payload.ExecutionInstanceId);
    }

    [Fact]
    public async Task Post_control_triggers_second_identical_AlertCreated_is_accepted_and_suppressed()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ChronoFlowIntakeTestFactory.IntakeApiKey);

        var body = ValidBody(alertId: Guid.NewGuid());
        var first = await client.PostAsJsonAsync("/control/triggers", body);
        var second = await client.PostAsJsonAsync("/control/triggers", body);

        first.EnsureSuccessStatusCode();
        second.EnsureSuccessStatusCode();
        var p1 = await first.Content.ReadFromJsonAsync<ControlTriggerAcceptedResponse>();
        var p2 = await second.Content.ReadFromJsonAsync<ControlTriggerAcceptedResponse>();
        Assert.NotNull(p1);
        Assert.NotNull(p2);
        Assert.True(p1!.WasExecuted);
        Assert.False(p1.WasSuppressed);
        Assert.False(p2!.WasExecuted);
        Assert.True(p2.WasSuppressed);
        Assert.NotNull(p2.SuppressionReason);
        Assert.Equal(0, p2.ExecutedStepCount);
        Assert.NotNull(p1.ExecutionRecordId);
        Assert.NotNull(p2.ExecutionRecordId);
        Assert.NotEqual(p1.ExecutionRecordId, p2.ExecutionRecordId);
        Assert.NotNull(p1.ExecutionInstanceId);
        Assert.Null(p2.ExecutionInstanceId);
    }

    [Fact]
    public async Task Post_control_triggers_returns_401_when_api_key_incorrect()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "wrong-key");

        var response = await client.PostAsJsonAsync("/control/triggers", ValidBody());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_control_triggers_returns_400_when_trigger_type_missing()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ChronoFlowIntakeTestFactory.IntakeApiKey);

        var invalid = ValidBody() with { TriggerType = " " };

        var response = await client.PostAsJsonAsync("/control/triggers", invalid);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_control_triggers_accepts_without_api_key_when_intake_key_not_configured()
    {
        await using var factory = new ControlIntakeWithoutApiKeyFactory();

        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/control/triggers", ValidBody());

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ControlTriggerAcceptedResponse>();
        Assert.NotNull(payload);
        Assert.True(payload!.WasExecuted);
        Assert.False(payload.WasSuppressed);
        Assert.Equal("alert-created-default", payload.WorkflowKey);
        Assert.Equal(3, payload.ExecutedStepCount);
        Assert.NotNull(payload.ExecutionRecordId);
        Assert.NotNull(payload.ExecutionInstanceId);
        Assert.NotEqual(Guid.Empty, payload.ExecutionInstanceId);
    }

    [Fact]
    public async Task Post_control_triggers_Accepted_body_reflects_no_execution_for_AlertAcknowledged()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ChronoFlowIntakeTestFactory.IntakeApiKey);

        var body = ValidBody() with
        {
            TriggerType = "AlertAcknowledged",
            LifecycleEventType = "AlertAcknowledged"
        };
        var response = await client.PostAsJsonAsync("/control/triggers", body);

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ControlTriggerAcceptedResponse>();
        Assert.NotNull(payload);
        Assert.False(payload!.WasExecuted);
        Assert.False(payload.WasSuppressed);
        Assert.Equal(0, payload.ExecutedStepCount);
        Assert.NotNull(payload.ExecutionRecordId);
        Assert.Null(payload.ExecutionInstanceId);
    }

    /// <summary>Unique alert id per call by default avoids cross-test interference from singleton in-memory suppression store.</summary>
    private static ReceiveControlTriggerRequest ValidBody(Guid? alertId = null) =>
        new(
            TriggerType: "AlertCreated",
            AlertId: alertId ?? Guid.NewGuid(),
            RuleId: Guid.Parse("b2000000-0000-4000-8000-000000000002"),
            SignalId: Guid.Parse("c3000000-0000-4000-8000-000000000003"),
            OccurredAtUtc: new DateTimeOffset(2026, 4, 3, 10, 0, 0, TimeSpan.Zero),
            CurrentStatus: "Open",
            LifecycleEventType: "AlertCreated",
            AcknowledgedByUserId: null,
            ResolvedByUserId: null,
            ReopenedByUserId: null,
            RuleName: "Test rule",
            HasBeenReopened: false);
}
