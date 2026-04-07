using ChronoFlow.Modules.ControlTriggers.Domain;

namespace ChronoFlow.Modules.ControlTriggers.Application;

public interface IControlExecutionRecordRepository
{
    Task AddAsync(ControlExecutionRecord record, CancellationToken cancellationToken = default);

    Task UpdateAsync(ControlExecutionRecord record, CancellationToken cancellationToken = default);

    Task<ControlExecutionRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ControlExecutionRecord>> ListAsync(
        ControlExecutionRecordQuery query,
        CancellationToken cancellationToken = default);
}
