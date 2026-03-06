using ChronoFlow.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace ChronoFlow.Modules.Identity.Infrastructure.Persistence;

public sealed class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("users");
            b.HasKey(x => x.Id);

            b.Property(x => x.Email).IsRequired();
            b.HasIndex(x => x.Email).IsUnique();

            b.Property(x => x.PasswordHash).IsRequired();
        });
    }
}