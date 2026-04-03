using ChronoFlow.Modules.ControlTriggers.Application;
using ChronoFlow.Tests.Application;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace ChronoFlow.Tests.Api;

/// <summary>Intake tests with advisory enabled and a fake AIL advisor (no real HTTP to AIL).</summary>
public sealed class AdvisoryControlIntakeTestFactory : WebApplicationFactory<Program>
{
    private readonly ControlAdvisoryOutcome _outcome;

    public AdvisoryControlIntakeTestFactory(ControlAdvisoryOutcome outcome)
    {
        _outcome = outcome;
    }

    public const string IntakeApiKey = ChronoFlowIntakeTestFactory.IntakeApiKey;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        var suffix = Guid.NewGuid().ToString("N");
        builder.UseSetting("Testing:EventsInMemoryDatabase", "intake-advisory-events-" + suffix);
        builder.UseSetting("Testing:IdentityInMemoryDatabase", "intake-advisory-identity-" + suffix);
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=localhost;Port=5432;Database=chronoflow_test;Username=postgres;Password=postgres");
        builder.UseSetting("Jwt:Issuer", "ChronoFlow");
        builder.UseSetting("Jwt:Audience", "ChronoFlow");
        builder.UseSetting(
            "Jwt:Key",
            "0123456789abcdef0123456789abcdef0123456789abcdef");
        builder.UseSetting("ControlTriggers:Intake:ExpectedApiKey", IntakeApiKey);
        builder.UseSetting("ControlTriggers:Advisory:Enabled", "true");
        builder.UseSetting("ControlTriggers:Advisory:BaseUrl", "http://127.0.0.1:9/");

        builder.ConfigureTestServices(services =>
        {
            foreach (var d in services.Where(d => d.ServiceType == typeof(IControlDecisionAdvisor)).ToList())
                services.Remove(d);
            services.AddSingleton<IControlDecisionAdvisor>(new FakeControlDecisionAdvisor(_outcome));
        });
    }
}
