using ChronoFlow.Modules.ControlTriggers.Domain;

namespace ChronoFlow.Modules.ControlTriggers.Application;

public static class GetControlExecutionById
{
    public sealed class Handler(IControlExecutionRecordRepository repository)
    {
        public Task<ControlExecutionRecord?> HandleAsync(Guid id, CancellationToken cancellationToken = default) =>
            repository.GetByIdAsync(id, cancellationToken);
    }
}
