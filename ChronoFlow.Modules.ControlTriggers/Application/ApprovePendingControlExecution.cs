using ChronoFlow.Modules.ControlTriggers.Domain;

namespace ChronoFlow.Modules.ControlTriggers.Application;

public static class ApprovePendingControlExecution
{
    public sealed class Handler(
        IControlExecutionRecordRepository executionRecords,
        IControlTriggerRouter router,
        IWorkflowExecutor executor,
        IControlTriggerDeduplicator deduplicator)
    {
        public async Task<PendingReviewActionResult> HandleAsync(
            Guid executionRecordId,
            string? operatorNote,
            CancellationToken cancellationToken = default)
        {
            var record = await executionRecords
                .GetByIdAsync(executionRecordId, cancellationToken)
                .ConfigureAwait(false);

            if (record is null)
                return PendingReviewActionResult.Fail(PendingReviewActionFailureKind.NotFound);

            var block = PendingOperatorReviewActionGuards.ClassifyBlocking(record);
            if (block is not null)
                return PendingReviewActionResult.Fail(block.Value);

            if (string.IsNullOrWhiteSpace(record.WorkflowKey) || record.ExecutionInstanceId is null)
                return PendingReviewActionResult.Fail(PendingReviewActionFailureKind.InvalidRecordState);

            record = await executionRecords
                .GetByIdAsync(executionRecordId, cancellationToken)
                .ConfigureAwait(false);
            if (record is null)
                return PendingReviewActionResult.Fail(PendingReviewActionFailureKind.NotFound);

            block = PendingOperatorReviewActionGuards.ClassifyBlocking(record);
            if (block is not null)
                return PendingReviewActionResult.Fail(block.Value);

            if (string.IsNullOrWhiteSpace(record.WorkflowKey) || record.ExecutionInstanceId is null)
                return PendingReviewActionResult.Fail(PendingReviewActionFailureKind.InvalidRecordState);

            var command = ControlExecutionRecordTriggerRebuilder.ToCommand(record);
            var hint = BuildAdvisoryHint(record);
            var definition = router.ResolveWorkflow(command, hint);
            if (definition is null || !string.Equals(definition.WorkflowKey, record.WorkflowKey, StringComparison.Ordinal))
                return PendingReviewActionResult.Fail(PendingReviewActionFailureKind.WorkflowResolutionMismatch);

            var execution = await executor
                .ExecuteAsync(record.ExecutionInstanceId.Value, definition, command, cancellationToken)
                .ConfigureAwait(false);

            var actionAt = DateTimeOffset.UtcNow;
            var boundedNote = ControlTriggerTraceFieldBounds.BoundedOperatorReviewNote(operatorNote);
            var updated = PendingOperatorReviewRecordMutations.ToApprovedExecuted(
                record,
                execution.ExecutedStepCount,
                actionAt,
                actionAt,
                boundedNote);

            await executionRecords.UpdateAsync(updated, cancellationToken).ConfigureAwait(false);
            deduplicator.RecordSuccessfulExecution(command);
            return PendingReviewActionResult.Ok(updated);
        }

        private static ControlAdvisoryRouteHint? BuildAdvisoryHint(ControlExecutionRecord record)
        {
            if (!record.AdvisoryWasUsed || string.IsNullOrWhiteSpace(record.AdvisoryStrategyKey))
                return null;
            return new ControlAdvisoryRouteHint(record.AdvisoryStrategyKey);
        }
    }
}
