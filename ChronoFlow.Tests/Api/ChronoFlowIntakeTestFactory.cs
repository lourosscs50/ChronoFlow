using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ChronoFlow.Tests.Api;

/// <summary>JWT + intake API key for control-trigger API tests (Events DB is in-memory when ASP.NET environment is Testing).</summary>
public sealed class ChronoFlowIntakeTestFactory : WebApplicationFactory<Program>
{
    public const string IntakeApiKey = "intake-test-service-key";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        var suffix = Guid.NewGuid().ToString("N");
        builder.UseSetting("Testing:EventsInMemoryDatabase", "intake-control-events-" + suffix);
        builder.UseSetting("Testing:IdentityInMemoryDatabase", "intake-control-identity-" + suffix);
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=localhost;Port=5432;Database=chronoflow_test;Username=postgres;Password=postgres");
        builder.UseSetting("Jwt:Issuer", "ChronoFlow");
        builder.UseSetting("Jwt:Audience", "ChronoFlow");
        builder.UseSetting(
            "Jwt:Key",
            "0123456789abcdef0123456789abcdef0123456789abcdef");
        builder.UseSetting("ControlTriggers:Intake:ExpectedApiKey", IntakeApiKey);
    }
}
