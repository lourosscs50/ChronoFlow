using ChronoFlow.Modules.ControlTriggers.Domain;
using ChronoFlow.Modules.Events.Domain;
using Microsoft.EntityFrameworkCore;

namespace ChronoFlow.Modules.Events.Infrastructure.Persistence;

public sealed class EventsDbContext : DbContext
{
    public EventsDbContext(DbContextOptions<EventsDbContext> options) : base(options)
    {
    }

    public DbSet<EventRecord> Events => Set<EventRecord>();

    public DbSet<ControlExecutionRecord> ControlExecutionRecords => Set<ControlExecutionRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new EventEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ControlExecutionRecordEntityConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}