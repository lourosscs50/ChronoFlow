using System.Text.Json;
using ChronoFlow.Api.Contracts.Control;
using Xunit;

namespace ChronoFlow.Tests.Api;

/// <summary>Phase 6.4C: intake JSON must bind to the same camelCase shape SignalForge emits (<see cref="JsonSerializerDefaults.Web"/>).</summary>
public sealed class ControlTriggerIntakeContractTests
{
    [Fact]
    public void ReceiveControlTriggerRequest_deserializes_signalforge_normalized_web_json()
    {
        const string json =
            """
            {
              "triggerType": "AlertReopened",
              "alertId": "f1d56d1a-0000-4000-8000-000000000001",
              "ruleId": "f1d56d1a-0000-4000-8000-000000000002",
              "signalId": "f1d56d1a-0000-4000-8000-000000000003",
              "occurredAtUtc": "2026-03-15T08:30:00+00:00",
              "currentStatus": "Open",
              "lifecycleEventType": "AlertReopened",
              "acknowledgedByUserId": "user-a",
              "resolvedByUserId": null,
              "reopenedByUserId": "user-r",
              "ruleName": "Heat rule",
              "hasBeenReopened": true
            }
            """;

        var req = JsonSerializer.Deserialize<ReceiveControlTriggerRequest>(
            json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(req);
        Assert.Equal("AlertReopened", req!.TriggerType);
        Assert.Equal(Guid.Parse("f1d56d1a-0000-4000-8000-000000000001"), req.AlertId);
        Assert.Equal("Open", req.CurrentStatus);
        Assert.Equal("AlertReopened", req.LifecycleEventType);
        Assert.Equal("user-a", req.AcknowledgedByUserId);
        Assert.Null(req.ResolvedByUserId);
        Assert.Equal("user-r", req.ReopenedByUserId);
        Assert.Equal("Heat rule", req.RuleName);
        Assert.True(req.HasBeenReopened);
    }
}
