using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ChronoFlow.Tests.Api;

/// <summary>Factory for tests where intake API key is disabled.</summary>
public sealed class ControlIntakeWithoutApiKeyFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        var suffix = Guid.NewGuid().ToString("N");
        builder.UseSetting("Testing:EventsInMemoryDatabase", "intake-no-apikey-events-" + suffix);
        builder.UseSetting("Testing:IdentityInMemoryDatabase", "intake-no-apikey-identity-" + suffix);
        builder.UseSetting("Jwt:Issuer", "ChronoFlow");
        builder.UseSetting("Jwt:Audience", "ChronoFlow");
        builder.UseSetting(
            "Jwt:Key",
            "0123456789abcdef0123456789abcdef0123456789abcdef");
        builder.UseSetting("ControlTriggers:Intake:ExpectedApiKey", "");
    }
}
