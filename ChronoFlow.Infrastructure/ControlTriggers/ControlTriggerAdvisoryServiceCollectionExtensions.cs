using ChronoFlow.Modules.ControlTriggers.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ChronoFlow.Infrastructure.ControlTriggers;

public static class ControlTriggerAdvisoryServiceCollectionExtensions
{
    public static IServiceCollection AddControlTriggerAdvisoryIntegration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ControlTriggersAdvisoryOptions>(
            configuration.GetSection(ControlTriggersAdvisoryOptions.SectionName));

        services.AddHttpClient<IControlDecisionAdvisor, HttpAilControlDecisionAdvisor>((sp, client) =>
        {
            var o = sp.GetRequiredService<IOptions<ControlTriggersAdvisoryOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(o.BaseUrl))
                client.BaseAddress = new Uri(o.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(o.TimeoutSeconds, 1, 120));
        });

        return services;
    }
}
