using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChronoFlow.Modules.ControlTriggers.Application;

public sealed class ReceiveControlTriggerHandler(
    ILogger<ReceiveControlTriggerHandler> logger,
    IOptions<ControlTriggersAdvisoryOptions> advisoryOptions,
    IControlTriggerDeduplicator deduplicator,
    IControlTriggerRouter router,
    IControlDecisionAdvisor advisor,
    IWorkflowExecutionPolicy workflowPolicy,
    IWorkflowExecutor executor,
    IControlExecutionRecordRepository executionRecords)
{
    private const int MaxAdvisoryReasonLength = 500;
    private const int MaxLinkedAilExecutionIdLength = 200;

    public async Task<ReceiveControlTriggerResult> HandleAsync(
        ReceiveControlTriggerCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.TriggerType))
            return ReceiveControlTriggerResult.Invalid("TriggerType is required.");

        if (string.IsNullOrWhiteSpace(command.LifecycleEventType))
            return ReceiveControlTriggerResult.Invalid("LifecycleEventType is required.");

        if (string.IsNullOrWhiteSpace(command.CurrentStatus))
            return ReceiveControlTriggerResult.Invalid("CurrentStatus is required.");

        if (command.AlertId == Guid.Empty)
            return ReceiveControlTriggerResult.Invalid("AlertId is required.");

        if (command.RuleId == Guid.Empty)
            return ReceiveControlTriggerResult.Invalid("RuleId is required.");

        if (command.SignalId == Guid.Empty)
            return ReceiveControlTriggerResult.Invalid("SignalId is required.");

        var inboundBounded = InboundDecisionIntakeMapper.Map(command.InboundDecision);
        var receivedAtUtc = DateTimeOffset.UtcNow;

        logger.LogInformation(
            "Control trigger intake accepted: alert {AlertId}, lifecycle {Lifecycle}, status {Status}.",
            command.AlertId,
            command.LifecycleEventType,
            command.CurrentStatus);

        var dedup = deduplicator.EvaluateBeforeRouting(command);
        if (dedup.WasSuppressed)
        {
            logger.LogInformation(
                "Control trigger suppressed as duplicate for alert {AlertId}, lifecycle {Lifecycle}.",
                command.AlertId,
                command.LifecycleEventType);
            var suppressedRecord = ControlExecutionRecordFactory.CreateSuppressed(
                command,
                dedup.SuppressionReason!,
                receivedAtUtc,
                inboundBounded);
            await executionRecords.AddAsync(suppressedRecord, cancellationToken).ConfigureAwait(false);
            return ReceiveControlTriggerResult.OkSuppressed(dedup.SuppressionReason!, suppressedRecord.Id);
        }

        var opts = advisoryOptions.Value;
        var preliminaryWorkflow = router.ResolveWorkflow(command, null);
        Guid? orchestrationExecutionInstanceId = preliminaryWorkflow is not null ? Guid.NewGuid() : null;

        ControlAdvisoryOutcome? advisoryOutcome = null;
        if (opts.Enabled && preliminaryWorkflow is not null && orchestrationExecutionInstanceId is not null)
        {
            var advisoryResult = await advisor
                .GetAdvisoryAsync(command, orchestrationExecutionInstanceId.Value, cancellationToken)
                .ConfigureAwait(false);

            advisoryOutcome = advisoryResult switch
            {
                AdvisoryExecutionResult.Succeeded s => s.Outcome,
                AdvisoryExecutionResult.Unavailable u =>
                    LogAndIgnoreAdvisory(logger, u.ReasonCode, unavailable: true),
                AdvisoryExecutionResult.Failed f =>
                    LogAndIgnoreAdvisory(logger, f.ReasonCode, unavailable: false),
                AdvisoryExecutionResult.SkippedNotRequested =>
                    null,
                _ => null
            };
        }

        var routeHint = advisoryOutcome is null
            ? null
            : new ControlAdvisoryRouteHint(advisoryOutcome.SelectedStrategyKey);

        var definition = router.ResolveWorkflow(command, routeHint);
        var advisorySnapshot = BuildStartTimeAdvisorySnapshot(inboundBounded, advisoryOutcome);

        if (definition is null)
        {
            var noWorkflowRecord = ControlExecutionRecordFactory.CreateNoWorkflow(
                command,
                receivedAtUtc,
                advisorySnapshot);
            await executionRecords.AddAsync(noWorkflowRecord, cancellationToken).ConfigureAwait(false);
            return ReceiveControlTriggerResult.OkNotExecuted(noWorkflowRecord.Id);
        }

        var policyInput = WorkflowPolicyInput.From(command, advisorySnapshot, definition.WorkflowKey);
        var policyDecision = workflowPolicy.Evaluate(policyInput);

        switch (policyDecision.Kind)
        {
            case WorkflowPolicyKind.Suppress:
            {
                logger.LogInformation(
                    "Orchestration policy suppressed execution for alert {AlertId}, lifecycle {Lifecycle}.",
                    command.AlertId,
                    command.LifecycleEventType);
                var policyRecord = ControlExecutionRecordFactory.CreateOrchestrationPolicyRecord(
                    command,
                    receivedAtUtc,
                    advisorySnapshot,
                    OrchestrationPolicyOutcomes.PolicySuppressed,
                    workflowKey: null,
                    executionInstanceId: null,
                    pendingOperatorReview: false);
                await executionRecords.AddAsync(policyRecord, cancellationToken).ConfigureAwait(false);
                return ReceiveControlTriggerResult.OkPolicySuppressed(policyRecord.Id);
            }
            case WorkflowPolicyKind.AdvisoryOnly:
            {
                logger.LogInformation(
                    "Orchestration policy advisory-only for alert {AlertId}, workflow {WorkflowKey}.",
                    command.AlertId,
                    definition.WorkflowKey);
                var advisoryRecord = ControlExecutionRecordFactory.CreateOrchestrationPolicyRecord(
                    command,
                    receivedAtUtc,
                    advisorySnapshot,
                    OrchestrationPolicyOutcomes.AdvisoryOnly,
                    definition.WorkflowKey,
                    executionInstanceId: null,
                    pendingOperatorReview: false);
                await executionRecords.AddAsync(advisoryRecord, cancellationToken).ConfigureAwait(false);
                return ReceiveControlTriggerResult.OkAdvisoryOnly(definition.WorkflowKey, advisoryRecord.Id);
            }
            case WorkflowPolicyKind.RequireReview:
            {
                var reviewInstanceId = orchestrationExecutionInstanceId ?? Guid.NewGuid();
                logger.LogInformation(
                    "Orchestration policy requires review for alert {AlertId}, workflow {WorkflowKey}, instance {InstanceId}.",
                    command.AlertId,
                    definition.WorkflowKey,
                    reviewInstanceId);
                var reviewRecord = ControlExecutionRecordFactory.CreateOrchestrationPolicyRecord(
                    command,
                    receivedAtUtc,
                    advisorySnapshot,
                    OrchestrationPolicyOutcomes.PendingReview,
                    definition.WorkflowKey,
                    reviewInstanceId,
                    pendingOperatorReview: true);
                await executionRecords.AddAsync(reviewRecord, cancellationToken).ConfigureAwait(false);
                return ReceiveControlTriggerResult.OkPendingReview(
                    definition.WorkflowKey,
                    reviewRecord.Id,
                    reviewInstanceId);
            }
            case WorkflowPolicyKind.Proceed:
            default:
                break;
        }

        var executionInstanceId = orchestrationExecutionInstanceId ?? Guid.NewGuid();
        var execution = await executor
            .ExecuteAsync(executionInstanceId, definition, command, cancellationToken)
            .ConfigureAwait(false);
        var executedAtUtc = DateTimeOffset.UtcNow;
        var executedRecord = ControlExecutionRecordFactory.CreateExecuted(
            command,
            definition.WorkflowKey,
            executionInstanceId,
            execution.ExecutedStepCount,
            receivedAtUtc,
            executedAtUtc,
            advisorySnapshot);
        await executionRecords.AddAsync(executedRecord, cancellationToken).ConfigureAwait(false);
        deduplicator.RecordSuccessfulExecution(command);
        return ReceiveControlTriggerResult.OkExecuted(
            definition.WorkflowKey,
            execution.ExecutedStepCount,
            executedRecord.Id,
            executionInstanceId);
    }

    private static ControlAdvisoryOutcome? LogAndIgnoreAdvisory(
        ILogger<ReceiveControlTriggerHandler> logger,
        string reasonCode,
        bool unavailable)
    {
        if (unavailable)
        {
            logger.LogWarning(
                "Control trigger advisory unavailable ({ReasonCode}); using default local routing.",
                reasonCode);
        }
        else
        {
            logger.LogWarning(
                "Control trigger advisory failed ({ReasonCode}); using default local routing.",
                reasonCode);
        }

        return null;
    }

    private static AdvisoryExecutionSnapshot BuildStartTimeAdvisorySnapshot(
        BoundedInboundDecisionSnapshot inbound,
        ControlAdvisoryOutcome? outcome)
    {
        if (outcome is null)
        {
            return new AdvisoryExecutionSnapshot(
                false,
                null,
                null,
                null,
                null,
                inbound.Summary,
                inbound.ReferenceId,
                inbound.Confidence,
                inbound.ReasonCode,
                inbound.LinkedExternalExecutionId);
        }

        return new AdvisoryExecutionSnapshot(
            true,
            outcome.SelectedStrategyKey,
            outcome.Confidence,
            Truncate(outcome.ReasonSummary, MaxAdvisoryReasonLength),
            Truncate(outcome.LinkedAilExecutionId, MaxLinkedAilExecutionIdLength),
            inbound.Summary,
            inbound.ReferenceId,
            inbound.Confidence,
            inbound.ReasonCode,
            inbound.LinkedExternalExecutionId);
    }

    private static string? Truncate(string? text, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;
        var t = text.Trim();
        return t.Length <= maxLen ? t : t[..maxLen];
    }
}
