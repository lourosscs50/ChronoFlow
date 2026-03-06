using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ChronoFlow.Tests.Api;

public sealed class AuthEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Me_Without_Token_Returns_Unauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_Login_And_Me_Work_End_To_End()
    {
        var client = _factory.CreateClient();

        var email = $"test-{Guid.NewGuid():N}@chronoflow.dev";
        const string password = "Password123!";

        var registerResponse = await client.PostAsJsonAsync("/auth/register", new
        {
            email,
            password
        });

        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await client.PostAsJsonAsync("/auth/login", new
        {
            email,
            password
        });

        loginResponse.EnsureSuccessStatusCode();

        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(loginBody);
        Assert.False(string.IsNullOrWhiteSpace(loginBody!.Token));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginBody.Token);

        var meResponse = await client.GetAsync("/me");

        meResponse.EnsureSuccessStatusCode();

        var meBody = await meResponse.Content.ReadFromJsonAsync<MeResponse>();

        Assert.NotNull(meBody);
        Assert.Equal(email, meBody!.Email);
        Assert.NotEqual(Guid.Empty, meBody.UserId);
    }

    [Fact]
    public async Task Login_With_Wrong_Password_Returns_Unauthorized()
    {
        var client = _factory.CreateClient();

        var email = $"test-{Guid.NewGuid():N}@chronoflow.dev";
        const string password = "Password123!";

        var registerResponse = await client.PostAsJsonAsync("/auth/register", new
        {
            email,
            password
        });

        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await client.PostAsJsonAsync("/auth/login", new
        {
            email,
            password = "WrongPassword123!"

        });
        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

        [Fact]
        public async Task Me_With_Invalid_Token_Returns_Unauthorized()
        {
            var client = _factory.CreateClient();

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", "not-a-real-token");

            var response = await client.GetAsync("/me");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    private sealed record LoginResponse(string Token);
    private sealed record MeResponse(Guid UserId, string Email);
        }

