using ChronoFlow.Modules.ControlTriggers.Domain;

namespace ChronoFlow.Modules.ControlTriggers.Application;

public static class ListControlExecutions
{
    public sealed class Handler(IControlExecutionRecordRepository repository)
    {
        public Task<IReadOnlyList<ControlExecutionRecord>> HandleAsync(
            ControlExecutionRecordQuery query,
            CancellationToken cancellationToken = default) =>
            repository.ListAsync(query, cancellationToken);
    }
}
