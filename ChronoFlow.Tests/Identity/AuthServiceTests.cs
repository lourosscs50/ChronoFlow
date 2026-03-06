using System.Threading;
using ChronoFlow.Modules.Identity.Application;
using ChronoFlow.Modules.Identity.Infrastructure;
using Xunit;

namespace ChronoFlow.Tests.Identity;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_Creates_New_User()
    {
        var repository = new InMemoryUserRepository();
        var passwordHasher = new Pbkdf2PasswordHasher();
        var authService = new AuthService(repository, passwordHasher);

        var command = new RegisterUserCommand(
            Email: "test@chronoflow.dev",
            Password: "Password123!"
        );

        var result = await authService.RegisterAsync(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.UserId);
        Assert.Equal("test@chronoflow.dev", result.Email);
    }

    [Fact]
    public async Task LoginAsync_Returns_Token_For_Valid_Credentials()
    {
        var repository = new InMemoryUserRepository();
        var passwordHasher = new Pbkdf2PasswordHasher();
        var authService = new AuthService(repository, passwordHasher);

        await authService.RegisterAsync(new RegisterUserCommand(
            Email: "login@chronoflow.dev",
            Password: "Password123!"
        ), CancellationToken.None);

        var result = await authService.LoginAsync(new LoginUserCommand(
            Email: "login@chronoflow.dev",
            Password: "Password123!"
        ), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.UserId);
        Assert.Equal("login@chronoflow.dev", result.Email);
    }

    [Fact]
    public async Task RegisterAsync_Rejects_Duplicate_Email()
    {
        var repository = new InMemoryUserRepository();
        var passwordHasher = new Pbkdf2PasswordHasher();
        var authService = new AuthService(repository, passwordHasher);

        await authService.RegisterAsync(new RegisterUserCommand(
            Email: "dup@chronoflow.dev",
            Password: "Password123!"
        ), CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await authService.RegisterAsync(new RegisterUserCommand(
                Email: "dup@chronoflow.dev",
                Password: "Password123!"
            ), CancellationToken.None);
        });
    }
}