using HackerNewsBestStories.Application;
using HackerNewsBestStories.Infrastructure;
using HackerNewsBestStories.WebApi;
using HackerNewsBestStories.WebApi.Features.BestStories;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddPresentation();
builder.Services.AddExceptionHandling();
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.EnableDarkMode();
        options.WithTheme(ScalarTheme.BluePlanet);
    });
}

app.UseSerilogRequestLogging();

app.UseExceptionHandler();
app.UseHttpsRedirection();

app.MapBestStoriesEndpoints();

app.Run();
