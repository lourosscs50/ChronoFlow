using ChronoFlow.Modules.ControlTriggers.Application;
using ChronoFlow.Modules.ControlTriggers.Domain;
using Microsoft.EntityFrameworkCore;

namespace ChronoFlow.Modules.Events.Infrastructure.Persistence;

public sealed class EfControlExecutionRecordRepository(EventsDbContext dbContext) : IControlExecutionRecordRepository
{
    private const int MaxPageSize = 100;

    public async Task AddAsync(ControlExecutionRecord record, CancellationToken cancellationToken = default)
    {
        dbContext.ControlExecutionRecords.Add(record);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ControlExecutionRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.ControlExecutionRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ControlExecutionRecord>> ListAsync(
        ControlExecutionRecordQuery query,
        CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(query.Take, 1, MaxPageSize);
        var skip = Math.Max(0, query.Skip);

        var q = dbContext.ControlExecutionRecords.AsNoTracking().AsQueryable();

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

        return await q
            .OrderByDescending(x => x.ReceivedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
