using ChronoFlow.Modules.Events.Domain;
using Microsoft.EntityFrameworkCore;

namespace ChronoFlow.Modules.Events.Infrastructure.Persistence;

public sealed class EventsDbContext : DbContext
{
    public EventsDbContext(DbContextOptions<EventsDbContext> options) : base(options)
    {
    }

    public DbSet<EventRecord> Events => Set<EventRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new EventEntityConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}