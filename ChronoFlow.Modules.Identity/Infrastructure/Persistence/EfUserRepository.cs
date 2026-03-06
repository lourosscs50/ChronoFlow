using ChronoFlow.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace ChronoFlow.Modules.Identity.Infrastructure.Persistence;

public sealed class EfUserRepository : IUserRepository
{
    private readonly IdentityDbContext _db;

    public EfUserRepository(IdentityDbContext db)
    {
        _db = db;
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct)
        => _db.Users.FirstOrDefaultAsync(x => x.Email == email, ct);

    public async Task AddAsync(User user, CancellationToken ct)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
    }
}