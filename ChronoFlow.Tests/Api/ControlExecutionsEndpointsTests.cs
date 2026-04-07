using System.Net;
using System.Net.Http.Json;
using ChronoFlow.Api.Contracts.Control;
using ChronoFlow.Modules.ControlTriggers.Application;
using Xunit;
using static ChronoFlow.Modules.ControlTriggers.Application.OrchestrationPolicyInboundReasonCodes;
using static ChronoFlow.Modules.ControlTriggers.Application.OrchestrationPolicyOutcomes;

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
        Assert.Null(detail.CorrelationId);
        Assert.False(detail.PendingOperatorReview);
        Assert.Equal(OrchestrationPolicyOutcomes.Proceed, detail.OrchestrationPolicyOutcome);
        Assert.Equal(executionInstanceId, detail.ExecutionInstanceId);
        Assert.NotEqual(detail.Id, detail.ExecutionInstanceId);
        Assert.Equal(new DateTimeOffset(2026, 4, 3, 10, 0, 0, TimeSpan.Zero), detail.OccurredAtUtc);
        Assert.Null(detail.OperatorReviewAction);

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
        Assert.Null(detail.CorrelationId);
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
        Assert.Null(detail.CorrelationId);
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
        Assert.Null(detail.CorrelationId);
    }

    [Fact]
    public async Task Post_trigger_with_correlation_id_surfaces_on_get_and_list()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ChronoFlowIntakeTestFactory.IntakeApiKey);

        var alertId = Guid.NewGuid();
        var body = TriggersBody(alertId) with { CorrelationId = "platform-trace-42" };
        var post = await client.PostAsJsonAsync("/control/triggers", body);
        post.EnsureSuccessStatusCode();
        var accepted = await post.Content.ReadFromJsonAsync<ControlTriggerAcceptedResponse>();
        Assert.NotNull(accepted?.ExecutionRecordId);
        var recordId = accepted!.ExecutionRecordId!.Value;

        var detail = await client.GetFromJsonAsync<ControlExecutionRecordResponse>(
            $"/control/executions/{recordId}");
        Assert.NotNull(detail);
        Assert.Equal("platform-trace-42", detail!.CorrelationId);

        var list = await client.GetFromJsonAsync<List<ControlExecutionRecordResponse>>(
            $"/control/executions?alertId={alertId}&wasExecuted=true");
        Assert.NotNull(list);
        var row = list!.Single(x => x.Id == recordId);
        Assert.Equal("platform-trace-42", row.CorrelationId);
    }

    [Fact]
    public async Task Require_review_then_approve_executes_and_exposes_operator_audit_fields()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ChronoFlowIntakeTestFactory.IntakeApiKey);

        var alertId = Guid.NewGuid();
        var body = TriggersBody(alertId) with
        {
            InboundDecision = new InboundDecisionIntakeRequest(null, null, null, RequireReview, null)
        };
        var post = await client.PostAsJsonAsync("/control/triggers", body);
        post.EnsureSuccessStatusCode();
        var accepted = await post.Content.ReadFromJsonAsync<ControlTriggerAcceptedResponse>();
        Assert.NotNull(accepted?.ExecutionRecordId);
        Assert.True(accepted!.PendingOperatorReview);
        Assert.Equal(PendingReview, accepted.OrchestrationPolicyOutcome);
        var recordId = accepted.ExecutionRecordId!.Value;

        var pendingDetail = await client.GetFromJsonAsync<ControlExecutionRecordResponse>(
            $"/control/executions/{recordId}");
        Assert.NotNull(pendingDetail);
        Assert.True(pendingDetail!.PendingOperatorReview);
        Assert.False(pendingDetail.WasExecuted);

        var approve = await client.PostAsJsonAsync(
            $"/control/executions/{recordId}/approve",
            new OperatorReviewActionRequest(Note: "go"));
        approve.EnsureSuccessStatusCode();
        var approvedBody = await approve.Content.ReadFromJsonAsync<ControlExecutionRecordResponse>();
        Assert.NotNull(approvedBody);
        Assert.True(approvedBody!.WasExecuted);
        Assert.False(approvedBody.PendingOperatorReview);
        Assert.Equal(Proceed, approvedBody.OrchestrationPolicyOutcome);
        Assert.Equal(OperatorReviewActions.Approved, approvedBody.OperatorReviewAction);
        Assert.Equal("go", approvedBody.OperatorReviewNote);
        Assert.NotNull(approvedBody.OperatorReviewActionAtUtc);

        var finalGet = await client.GetFromJsonAsync<ControlExecutionRecordResponse>(
            $"/control/executions/{recordId}");
        Assert.NotNull(finalGet);
        Assert.Equal(approvedBody.OperatorReviewAction, finalGet!.OperatorReviewAction);
    }

    [Fact]
    public async Task Approve_after_cancel_returns_409_with_finalized_message()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ChronoFlowIntakeTestFactory.IntakeApiKey);

        var body = TriggersBody(Guid.NewGuid()) with
        {
            InboundDecision = new InboundDecisionIntakeRequest(null, null, null, RequireReview, null)
        };
        var post = await client.PostAsJsonAsync("/control/triggers", body);
        post.EnsureSuccessStatusCode();
        var accepted = await post.Content.ReadFromJsonAsync<ControlTriggerAcceptedResponse>();
        var recordId = accepted!.ExecutionRecordId!.Value;

        var cancel = await client.PostAsJsonAsync(
            $"/control/executions/{recordId}/cancel",
            new OperatorReviewActionRequest());
        cancel.EnsureSuccessStatusCode();

        var approve = await client.PostAsJsonAsync(
            $"/control/executions/{recordId}/approve",
            new OperatorReviewActionRequest());
        Assert.Equal(HttpStatusCode.Conflict, approve.StatusCode);
        var text = await approve.Content.ReadAsStringAsync();
        Assert.Contains("already recorded", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Approve_on_non_pending_record_returns_409()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ChronoFlowIntakeTestFactory.IntakeApiKey);

        var post = await client.PostAsJsonAsync("/control/triggers", TriggersBody(Guid.NewGuid()));
        post.EnsureSuccessStatusCode();
        var accepted = await post.Content.ReadFromJsonAsync<ControlTriggerAcceptedResponse>();
        var recordId = accepted!.ExecutionRecordId!.Value;

        var approve = await client.PostAsJsonAsync(
            $"/control/executions/{recordId}/approve",
            new OperatorReviewActionRequest());
        Assert.Equal(HttpStatusCode.Conflict, approve.StatusCode);
    }

    [Fact]
    public async Task Require_review_cancel_surfaces_review_cancelled_without_execution()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ChronoFlowIntakeTestFactory.IntakeApiKey);

        var body = TriggersBody(Guid.NewGuid()) with
        {
            InboundDecision = new InboundDecisionIntakeRequest(null, null, null, RequireReview, null)
        };
        var post = await client.PostAsJsonAsync("/control/triggers", body);
        post.EnsureSuccessStatusCode();
        var accepted = await post.Content.ReadFromJsonAsync<ControlTriggerAcceptedResponse>();
        var recordId = accepted!.ExecutionRecordId!.Value;

        var cancel = await client.PostAsJsonAsync(
            $"/control/executions/{recordId}/cancel",
            new OperatorReviewActionRequest(Note: "reject"));
        cancel.EnsureSuccessStatusCode();
        var row = await cancel.Content.ReadFromJsonAsync<ControlExecutionRecordResponse>();
        Assert.NotNull(row);
        Assert.False(row!.WasExecuted);
        Assert.False(row.PendingOperatorReview);
        Assert.Equal(ReviewCancelled, row.OrchestrationPolicyOutcome);
        Assert.Equal(OperatorReviewActions.Cancelled, row.OperatorReviewAction);
        Assert.Equal("reject", row.OperatorReviewNote);
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
