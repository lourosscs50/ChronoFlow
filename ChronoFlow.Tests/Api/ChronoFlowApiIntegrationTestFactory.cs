using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ChronoFlow.Tests.Api;

/// <summary>
/// Auth and Events API tests: Testing environment with isolated in-memory EF databases per fixture instance
/// (no shared PostgreSQL users/events tables across parallel runs).
/// </summary>
public sealed class ChronoFlowApiIntegrationTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var suffix = Guid.NewGuid().ToString("N");
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection",
            "Host=localhost;Port=5432;Database=chronoflow_integration_unused;Username=postgres;Password=postgres");
        builder.UseSetting("Testing:EventsInMemoryDatabase", "api-integration-events-" + suffix);
        builder.UseSetting("Testing:IdentityInMemoryDatabase", "api-integration-identity-" + suffix);
        builder.UseSetting("Jwt:Issuer", "ChronoFlow");
        builder.UseSetting("Jwt:Audience", "ChronoFlow");
        builder.UseSetting(
            "Jwt:Key",
            "0123456789abcdef0123456789abcdef0123456789abcdef");
        builder.UseSetting("ControlTriggers:Intake:ExpectedApiKey", "");
    }
}
