using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

/*

// WebAPI mit Authentifizierung verbinden
var api = builder.AddProject<Projects.MyWebApi>("webapi")
    .WithEnvironment("AzureAd__Instance", "https://login.microsoftonline.com/")
    .WithEnvironment("AzureAd__TenantId", azureAd.TenantId)
    .WithEnvironment("AzureAd__ClientId", azureAd.ClientId);

// WebApp mit Authentifizierung verbinden
var webApp = builder.AddProject<Projects.MyWebApp>("webapp")
    .WithEnvironment("AzureAd__Instance", "https://login.microsoftonline.com/")
    .WithEnvironment("AzureAd__TenantId", azureAd.TenantId)
    .WithEnvironment("AzureAd__ClientId", azureAd.ClientId);
*/
var cache = builder.AddRedis("cache");
var tables = builder.AddAzureStorage("storage").RunAsEmulator().AddTables("tables");

var openai = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureOpenAI("openAiConnection")
    : builder.AddConnectionString("openAiConnection");

var apiService = builder.AddProject<Projects.jarvis_ApiService>("apiservice")
    .WithReference(tables).WaitFor(tables).WithReference(openai).WithReference(cache).WaitFor(cache);

var webfrontend = builder.AddProject<Projects.jarvis_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithEnvironment("apiService", apiService.GetEndpoint("https"));

//apiService.WithEnvironment("webFrontend", webfrontend.GetEndpoint("https+http"));

builder.Build().Run();
