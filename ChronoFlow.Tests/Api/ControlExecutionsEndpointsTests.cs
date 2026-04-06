using System.Net;
using System.Net.Http.Json;
using ChronoFlow.Api.Contracts.Control;
using Xunit;

namespace ChronoFlow.Tests.Api;

public sealed class ControlExecutionsEndpointsTests : IClassFixture<ChronoFlowIntakeTestFactory>
{
    private readonly ChronoFlowIntakeTestFactory _factory;

    public ControlExecutionsEndpointsTests(ChronoFlowIntakeTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_control_executions_returns_401_when_api_key_missing()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/control/executions");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_control_executions_by_id_returns_404_when_missing()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ChronoFlowIntakeTestFactory.IntakeApiKey);
        var response = await client.GetAsync($"/control/executions/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_trigger_then_get_executions_surface_operator_fields_and_semantics()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ChronoFlowIntakeTestFactory.IntakeApiKey);

        var alertId = Guid.NewGuid();
        var post = await client.PostAsJsonAsync("/control/triggers", TriggersBody(alertId));
        post.EnsureSuccessStatusCode();
        var accepted = await post.Content.ReadFromJsonAsync<ControlTriggerAcceptedResponse>();
        Assert.NotNull(accepted?.ExecutionRecordId);
        var recordId = accepted!.ExecutionRecordId!.Value;
        Assert.NotNull(accepted.ExecutionInstanceId);
        var executionInstanceId = accepted.ExecutionInstanceId!.Value;

        var detailResponse = await client.GetAsync($"/control/executions/{recordId}");
        detailResponse.EnsureSuccessStatusCode();
        var detail = await detailResponse.Content.ReadFromJsonAsync<ControlExecutionRecordResponse>();
        Assert.NotNull(detail);
        Assert.Equal(recordId, detail!.Id);
        Assert.Equal(alertId, detail.AlertId);
        Assert.True(detail.WasExecuted);
        Assert.False(detail.WasSuppressed);
        Assert.Null(detail.SuppressionReason);
        Assert.NotNull(detail.WorkflowKey);
        Assert.True(detail.ExecutedStepCount > 0);
        Assert.NotNull(detail.ExecutedAtUtc);
        Assert.False(detail.AdvisoryWasUsed);
        Assert.Null(detail.AdvisoryStrategyKey);
        Assert.Null(detail.LinkedAilExecutionId);
        Assert.Null(detail.InboundDecisionSummary);
        Assert.Null(detail.InboundDecisionReferenceId);
        Assert.Equal(executionInstanceId, detail.ExecutionInstanceId);
        Assert.NotEqual(detail.Id, detail.ExecutionInstanceId);

        var listResponse = await client.GetAsync(
            $"/control/executions?alertId={alertId}&wasExecuted=true");
        listResponse.EnsureSuccessStatusCode();
        var list = await listResponse.Content.ReadFromJsonAsync<List<ControlExecutionRecordResponse>>();
        Assert.NotNull(list);
        Assert.Contains(list!, x => x.Id == recordId);
    }

    [Fact]
    public async Task Suppressed_second_post_exposes_suppressed_execution_record_shape()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ChronoFlowIntakeTestFactory.IntakeApiKey);

        var body = TriggersBody(Guid.NewGuid());
        var first = await client.PostAsJsonAsync("/control/triggers", body);
        var second = await client.PostAsJsonAsync("/control/triggers", body);
        first.EnsureSuccessStatusCode();
        second.EnsureSuccessStatusCode();

        var p2 = await second.Content.ReadFromJsonAsync<ControlTriggerAcceptedResponse>();
        Assert.NotNull(p2?.ExecutionRecordId);
        var recordId = p2!.ExecutionRecordId!.Value;

        var detail = await client.GetFromJsonAsync<ControlExecutionRecordResponse>($"/control/executions/{recordId}");
        Assert.NotNull(detail);
        Assert.False(detail!.WasExecuted);
        Assert.True(detail.WasSuppressed);
        Assert.NotNull(detail.SuppressionReason);
        Assert.Null(detail.WorkflowKey);
        Assert.Equal(0, detail.ExecutedStepCount);
        Assert.Null(detail.ExecutedAtUtc);
        Assert.False(detail.AdvisoryWasUsed);
        Assert.Null(detail.ExecutionInstanceId);
    }

    [Fact]
    public async Task AlertAcknowledged_persists_no_workflow_execution_record()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ChronoFlowIntakeTestFactory.IntakeApiKey);

        var post = await client.PostAsJsonAsync("/control/triggers", TriggersBody(Guid.NewGuid()) with
        {
            TriggerType = "AlertAcknowledged",
            LifecycleEventType = "AlertAcknowledged"
        });
        post.EnsureSuccessStatusCode();
        var accepted = await post.Content.ReadFromJsonAsync<ControlTriggerAcceptedResponse>();
        Assert.NotNull(accepted?.ExecutionRecordId);

        var detail = await client.GetFromJsonAsync<ControlExecutionRecordResponse>(
            $"/control/executions/{accepted!.ExecutionRecordId}");
        Assert.NotNull(detail);
        Assert.False(detail!.WasExecuted);
        Assert.False(detail.WasSuppressed);
        Assert.Null(detail.WorkflowKey);
        Assert.Equal(0, detail.ExecutedStepCount);
        Assert.Null(detail.ExecutedAtUtc);
        Assert.False(detail.AdvisoryWasUsed);
        Assert.Null(detail.ExecutionInstanceId);
    }

    [Fact]
    public async Task Post_trigger_with_inbound_decision_persists_bounded_fields_on_get()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ChronoFlowIntakeTestFactory.IntakeApiKey);

        var alertId = Guid.NewGuid();
        var body = TriggersBody(alertId) with
        {
            InboundDecision = new InboundDecisionIntakeRequest(
                "intake-summary",
                "intake-ref-1",
                "Low",
                "UPSTREAM_CODE",
                "ext-exec-zz")
        };
        var post = await client.PostAsJsonAsync("/control/triggers", body);
        post.EnsureSuccessStatusCode();
        var accepted = await post.Content.ReadFromJsonAsync<ControlTriggerAcceptedResponse>();
        Assert.NotNull(accepted?.ExecutionRecordId);

        var detail = await client.GetFromJsonAsync<ControlExecutionRecordResponse>(
            $"/control/executions/{accepted!.ExecutionRecordId}");
        Assert.NotNull(detail);
        Assert.False(detail!.AdvisoryWasUsed);
        Assert.Equal("intake-summary", detail.InboundDecisionSummary);
        Assert.Equal("intake-ref-1", detail.InboundDecisionReferenceId);
        Assert.Equal("Low", detail.InboundDecisionConfidence);
        Assert.Equal("UPSTREAM_CODE", detail.InboundDecisionReasonCode);
        Assert.Equal("ext-exec-zz", detail.InboundLinkedExternalExecutionId);
        Assert.Null(detail.LinkedAilExecutionId);
    }

    private static ReceiveControlTriggerRequest TriggersBody(Guid alertId) =>
        new(
            TriggerType: "AlertCreated",
            AlertId: alertId,
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
