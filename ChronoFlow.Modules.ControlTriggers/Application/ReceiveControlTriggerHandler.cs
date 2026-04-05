using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChronoFlow.Modules.ControlTriggers.Application;

public sealed class ReceiveControlTriggerHandler(
    ILogger<ReceiveControlTriggerHandler> logger,
    IOptions<ControlTriggersAdvisoryOptions> advisoryOptions,
    IControlTriggerDeduplicator deduplicator,
    IControlTriggerRouter router,
    IControlDecisionAdvisor advisor,
    IWorkflowExecutor executor,
    IControlExecutionRecordRepository executionRecords)
{
    private const int MaxAdvisoryReasonLength = 500;

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
                receivedAtUtc);
            await executionRecords.AddAsync(suppressedRecord, cancellationToken).ConfigureAwait(false);
            return ReceiveControlTriggerResult.OkSuppressed(dedup.SuppressionReason!, suppressedRecord.Id);
        }

        var opts = advisoryOptions.Value;
        var preliminaryWorkflow = router.ResolveWorkflow(command, null);
        ControlAdvisoryOutcome? advisoryOutcome = null;
        if (opts.Enabled && preliminaryWorkflow is not null)
        {
            advisoryOutcome = await advisor
                .GetAdvisoryAsync(command, cancellationToken)
                .ConfigureAwait(false);
        }

        var routeHint = advisoryOutcome is null
            ? null
            : new ControlAdvisoryRouteHint(advisoryOutcome.SelectedStrategyKey);

        var definition = router.ResolveWorkflow(command, routeHint);
        var advisorySnapshot = ToAdvisorySnapshot(advisoryOutcome);

        if (definition is null)
        {
            var noWorkflowRecord = ControlExecutionRecordFactory.CreateNoWorkflow(command, receivedAtUtc);
            await executionRecords.AddAsync(noWorkflowRecord, cancellationToken).ConfigureAwait(false);
            return ReceiveControlTriggerResult.OkNotExecuted(noWorkflowRecord.Id);
        }

        var executionInstanceId = Guid.NewGuid();
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

    private static AdvisoryExecutionSnapshot ToAdvisorySnapshot(ControlAdvisoryOutcome? outcome)
    {
        if (outcome is null)
            return new AdvisoryExecutionSnapshot(false, null, null, null);

        return new AdvisoryExecutionSnapshot(
            AdvisoryWasUsed: true,
            AdvisoryStrategyKey: outcome.SelectedStrategyKey,
            AdvisoryConfidence: outcome.Confidence,
            AdvisoryReasonSummary: Truncate(outcome.ReasonSummary, MaxAdvisoryReasonLength));
    }

    private static string? Truncate(string? text, int maxLen)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        return text.Length <= maxLen ? text : text[..maxLen];
    }
}
