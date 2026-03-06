using ChronoFlow.Modules.Identity.Domain;
using ChronoFlow.Modules.Identity.Infrastructure;

namespace ChronoFlow.Modules.Identity.Application;

public class AuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;

    public AuthService(IUserRepository users, IPasswordHasher hasher)
    {
        _users = users;
        _hasher = hasher;
    }

    public async Task<(Guid UserId, string Email)> RegisterAsync(RegisterUserCommand command, CancellationToken ct)
    {
        var email = (command.Email ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.");
        if (string.IsNullOrWhiteSpace(command.Password)) throw new ArgumentException("Password is required.");

        var existing = await _users.GetByEmailAsync(email, ct);
        if (existing is not null)
            throw new InvalidOperationException("User with this email already exists.");

        var hash = _hasher.HashPassword(command.Password);
        var user = new User(Guid.NewGuid(), email, hash);

        await _users.AddAsync(user, ct);

        return (user.Id, user.Email);
    }

    public async Task<(Guid UserId, string Email)> LoginAsync(LoginUserCommand command, CancellationToken ct)
    {
        var email = (command.Email ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email)) throw new InvalidOperationException("Invalid email or password.");
        if (string.IsNullOrWhiteSpace(command.Password)) throw new InvalidOperationException("Invalid email or password.");

        var existing = await _users.GetByEmailAsync(email, ct);
        if (existing is null) throw new InvalidOperationException("Invalid email or password.");

        var ok = _hasher.VerifyPassword(command.Password, existing.PasswordHash);
        if (!ok) throw new InvalidOperationException("Invalid email or password.");

        return (existing.Id, existing.Email);
    }
}