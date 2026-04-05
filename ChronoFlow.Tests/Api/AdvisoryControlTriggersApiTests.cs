using System.Net.Http.Json;
using ChronoFlow.Api.Contracts.Control;
using ChronoFlow.Modules.ControlTriggers.Application;
using Xunit;

namespace ChronoFlow.Tests.Api;

public sealed class AdvisoryControlTriggersApiTests
{
    [Fact]
    public async Task Post_control_triggers_advisory_memory_informed_maps_to_memory_workflow_and_record()
    {
        await using var factory = new AdvisoryControlIntakeTestFactory(
            new ControlAdvisoryOutcome(AdvisoryStrategyKeys.MemoryInformed, "High", "memory hint", false, 0));
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", AdvisoryControlIntakeTestFactory.IntakeApiKey);

        var alertId = Guid.NewGuid();
        var response = await client.PostAsJsonAsync("/control/triggers", ValidBody(alertId));
        response.EnsureSuccessStatusCode();

        var accepted = await response.Content.ReadFromJsonAsync<ControlTriggerAcceptedResponse>();
        Assert.NotNull(accepted);
        Assert.True(accepted!.WasExecuted);
        Assert.Equal("alert-created-memory-informed", accepted.WorkflowKey);
        Assert.NotNull(accepted.ExecutionRecordId);
        Assert.NotNull(accepted.ExecutionInstanceId);

        var detail = await client.GetFromJsonAsync<ControlExecutionRecordResponse>(
            $"/control/executions/{accepted.ExecutionRecordId}");
        Assert.NotNull(detail);
        Assert.True(detail!.AdvisoryWasUsed);
        Assert.Equal(AdvisoryStrategyKeys.MemoryInformed, detail.AdvisoryStrategyKey);
        Assert.Equal("High", detail.AdvisoryConfidence);
        Assert.Equal("memory hint", detail.AdvisoryReasonSummary);
        Assert.Equal(accepted.ExecutionInstanceId, detail.ExecutionInstanceId);
    }

    private static ReceiveControlTriggerRequest ValidBody(Guid alertId) =>
        new(
            TriggerType: "AlertCreated",
            AlertId: alertId,
            RuleId: Guid.Parse("b2000000-0000-4000-8000-000000000002"),
            SignalId: Guid.Parse("c3000000-0000-4000-8000-000000000003"),
            OccurredAtUtc: new DateTimeOffset(2026, 4, 3, 14, 0, 0, TimeSpan.Zero),
            CurrentStatus: "Open",
            LifecycleEventType: "AlertCreated",
            AcknowledgedByUserId: null,
            ResolvedByUserId: null,
            ReopenedByUserId: null,
            RuleName: "Advisory rule",
            HasBeenReopened: false);
}
