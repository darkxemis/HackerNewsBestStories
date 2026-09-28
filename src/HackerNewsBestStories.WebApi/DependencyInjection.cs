namespace HackerNewsBestStories.WebApi;

using HackerNewsBestStories.WebApi.Middleware;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "Hacker News Best Stories API",
                    Version = "v1",
                    Description =
                        "Returns the top stories from Hacker News ordered by score. " +
                        "Built with ASP.NET Core Minimal APIs, CQRS and a result-based error model.",
                };

                return Task.CompletedTask;
            });
        });

        return services;
    }

    public static IServiceCollection AddExceptionHandling(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }
}
