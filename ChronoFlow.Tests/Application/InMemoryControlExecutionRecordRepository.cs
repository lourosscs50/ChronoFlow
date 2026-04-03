using ChronoFlow.Modules.ControlTriggers.Application;
using ChronoFlow.Modules.ControlTriggers.Domain;

namespace ChronoFlow.Tests.Application;

/// <summary>In-memory store for application-layer tests (not for production).</summary>
public sealed class InMemoryControlExecutionRecordRepository : IControlExecutionRecordRepository
{
    private readonly List<ControlExecutionRecord> _records = [];

    public IReadOnlyList<ControlExecutionRecord> Records => _records;

    public Task AddAsync(ControlExecutionRecord record, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _records.Add(record);
        return Task.CompletedTask;
    }

    public Task<ControlExecutionRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_records.FirstOrDefault(x => x.Id == id));
    }

    public Task<IReadOnlyList<ControlExecutionRecord>> ListAsync(
        ControlExecutionRecordQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var q = _records.AsEnumerable();

        if (query.AlertId.HasValue)
            q = q.Where(x => x.AlertId == query.AlertId.Value);

        if (!string.IsNullOrWhiteSpace(query.LifecycleEventType))
        {
            var lt = query.LifecycleEventType.Trim();
            q = q.Where(x => x.LifecycleEventType == lt);
        }

        if (query.WasExecuted.HasValue)
            q = q.Where(x => x.WasExecuted == query.WasExecuted.Value);

        if (query.WasSuppressed.HasValue)
            q = q.Where(x => x.WasSuppressed == query.WasSuppressed.Value);

        if (!string.IsNullOrWhiteSpace(query.WorkflowKey))
        {
            var wk = query.WorkflowKey.Trim();
            q = q.Where(x => x.WorkflowKey == wk);
        }

        var take = Math.Clamp(query.Take, 1, 100);
        var skip = Math.Max(0, query.Skip);
        var list = q
            .OrderByDescending(x => x.ReceivedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToList();

        return Task.FromResult<IReadOnlyList<ControlExecutionRecord>>(list);
    }
}
