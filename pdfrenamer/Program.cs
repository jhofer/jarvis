using Microsoft.AspNetCore.Authentication;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PDFRenamer.Services;
using PDFRenamerIsolated.Services;
using System.Linq;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services
    .AddAzureClients(clientBuilder =>
    {
        var connectionString = builder.Configuration.GetValue<string>("AzureWebJobsStorage");
        clientBuilder.AddBlobServiceClient(connectionString);
        clientBuilder.AddTableServiceClient(connectionString);


    });


builder.Services
.AddSingleton<IAccessRepository, StorageAccessRepository>()
.AddSingleton<ITokenProvider, TokenProvider>()
.AddSingleton<IOneDrive, OneDrive>()
.AddSingleton<ICache, Cache>()
.AddSingleton<IAI, AI>();

builder.Services
.AddScoped<ClaimsProvider>();


builder
    .UseWhen<ClaimsProviderMiddleware>((context) =>
    {
        // We want to use this middleware only for http trigger invocations.
        return context.FunctionDefinition.InputBindings.Values
                        .First(a => a.Type.EndsWith("Trigger")).Type == "httpTrigger";
    });

// Registers IHttpClientFactory.
// By default this sends a lot of Information-level logs.
builder.Services.AddHttpClient();

// Disable IHttpClientFactory Informational logs.
// Note -- you can also remove the handler that does the logging: https://github.com/aspnet/HttpClientFactory/issues/196#issuecomment-432755765 
builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);


builder.Logging.Services.Configure<LoggerFilterOptions>(options =>
{
    // The Application Insights SDK adds a default logging filter that instructs ILogger to capture only Warning and more severe logs. Application Insights requires an explicit override.
    // Log levels can also be configured using appsettings.json. For more information, see https://learn.microsoft.com/azure/azure-monitor/app/worker-service#ilogger-logs
    LoggerFilterRule defaultRule = options.Rules.FirstOrDefault(rule => rule.ProviderName
        == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");
    if (defaultRule is not null)
    {
        options.Rules.Remove(defaultRule);
    }

});

var host = builder.Build();

await host.RunAsync();