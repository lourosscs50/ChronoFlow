using ChronoFlow.Modules.Identity.Domain;

namespace ChronoFlow.Modules.Identity.Infrastructure;   

public class InMemoryUserRepository : IUserRepository
{
    private readonly Dictionary<string, User> _users = new();
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct)
    {
        _users.TryGetValue(email, out var user);
        return Task.FromResult(user);
    }

    public Task AddAsync(User user, CancellationToken ct)
    {
        _users[user.Email] = user;
        return Task.CompletedTask;
    }
}