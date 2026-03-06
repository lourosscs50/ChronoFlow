namespace ChronoFlow.Modules.Identity.Infrastructure;

using ChronoFlow.Modules.Identity.Domain;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);
}