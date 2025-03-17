using jarvis.ApiService;
using jarvis.ApiService.Integrations;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Identity.Web;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.AddAzureOpenAIClient("openAiConnection");
builder.AddAzureTableClient("tables");
// Add services to the container.
builder.Services.AddProblemDetails();

builder.Services.AddControllers();

builder.AddRedisClient(connectionName: "cache");
builder.Services.AddAppServices();

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
       .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));


/*builder
    .UseWhen<ClaimsProviderMiddleware>((context) =>
    {
        // We want to use this middleware only for http trigger invocations.
        return context.FunctionDefinition.InputBindings.Values
                        .First(a => a.Type.EndsWith("Trigger")).Type == "httpTrigger";
    });*/


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => // UseSwaggerUI is called only in Development.
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = string.Empty;
    });
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();



app.MapDefaultEndpoints();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

//app.UseAuthorization();

app.Run();

