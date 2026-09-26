namespace HackerNewsBestStories.Infrastructure;

using HackerNewsBestStories.Application.Common.Interfaces;
using HackerNewsBestStories.Infrastructure.HackerNews;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<HackerNewsOptions>(configuration.GetSection(HackerNewsOptions.SectionName));

        var hackerNewsOptions = configuration.GetSection(HackerNewsOptions.SectionName).Get<HackerNewsOptions>()
            ?? new HackerNewsOptions();

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<HackerNewsOptions>>().Value;

            return new HackerNewsConcurrencyGate(options.MaxConcurrentRequests);
        });

        services.AddHttpClient(HackerNewsApiClient.ApiClientName, (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<HackerNewsOptions>>().Value;

            client.BaseAddress = new Uri(options.BaseAddress);
            client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
        })
        .AddStandardResilienceHandler(options =>
        {
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(hackerNewsOptions.RequestTimeoutSeconds);
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(hackerNewsOptions.TotalTimeoutSeconds);
        });

        services.AddSingleton<IHackerNewsApiClient, HackerNewsApiClient>();

        return services;
    }
}
