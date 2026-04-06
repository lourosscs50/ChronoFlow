using ChronoFlow.Modules.ControlTriggers.Domain;

namespace ChronoFlow.Modules.ControlTriggers.Application;

public static class CancelPendingControlExecution
{
    public sealed class Handler(IControlExecutionRecordRepository executionRecords)
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

            if (!IsEligiblePendingReview(record))
                return PendingReviewActionResult.Fail(PendingReviewActionFailureKind.NotPendingReview);

            var actionAt = DateTimeOffset.UtcNow;
            var boundedNote = ControlTriggerTraceFieldBounds.BoundedOperatorReviewNote(operatorNote);
            var updated = PendingOperatorReviewRecordMutations.ToCancelled(record, actionAt, boundedNote);
            await executionRecords.UpdateAsync(updated, cancellationToken).ConfigureAwait(false);
            return PendingReviewActionResult.Ok(updated);
        }

        private static bool IsEligiblePendingReview(ControlExecutionRecord record) =>
            record.PendingOperatorReview
            && string.Equals(record.OrchestrationPolicyOutcome, OrchestrationPolicyOutcomes.PendingReview, StringComparison.Ordinal)
            && !record.WasExecuted
            && record.OperatorReviewAction is null;
    }
}
