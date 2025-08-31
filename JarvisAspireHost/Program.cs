
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var acr = builder.AddAzureContainerRegistry("my-acr");

// Load environment variables from .env file for local development
var envFile = Path.Combine(builder.AppHostDirectory, ".env");
if (File.Exists(envFile))
{
    var lines = File.ReadAllLines(envFile);
    foreach (var line in lines)
    {
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
            continue;

        var parts = line.Split('=', 2);
        if (parts.Length == 2)
        {
            Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
        }
    }
}

var kv = builder.AddAzureKeyVault("kv");

var frontend = builder.AddDockerfile("nextjs-frontend", "../nextjs-frontend")
    .WithHttpEndpoint(env: "PORT", targetPort: 3000, port: 3000)
    .WithExternalHttpEndpoints()
    .WithEnvironment("AZURE_AD_CLIENT_ID", "b953e5d9-5032-41d8-afa6-ca702ec2f5eb")
    .WithEnvironment("AZURE_AD_TENANT_ID", "98dff5a3-e183-41eb-8738-8a097e0f4e95");

// For local development, use environment variables; for production, use Key Vault
if (builder.Environment.IsDevelopment())
{
    frontend
        .WithEnvironment("AZURE_AD_CLIENT_SECRET", Environment.GetEnvironmentVariable("AZURE_AD_CLIENT_SECRET") ?? builder.Configuration["AZURE_AD_CLIENT_SECRET"]!)
        .WithEnvironment("NEXTAUTH_SECRET", Environment.GetEnvironmentVariable("NEXTAUTH_SECRET") ?? builder.Configuration["NEXTAUTH_SECRET"]!);
}
else
{
    frontend
        .WithEnvironment("AZURE_AD_CLIENT_SECRET", kv.GetSecret("AZURE-AD-CLIENT-SECRET"))
        .WithEnvironment("NEXTAUTH_SECRET", kv.GetSecret("NEXTAUTH-SECRET"));
}

frontend.WithReference(kv);

var redirectUrl = builder.Environment.IsDevelopment() ? $"http://localhost:{3000}" : frontend.GetEndpoint("https").Url;
Console.WriteLine($"Redirect URL: {redirectUrl}");
frontend.WithEnvironment("NEXTAUTH_URL", redirectUrl);//"https://nextjs-frontend.whitepebble-d22a3c98.westeurope.azurecontainerapps.io"

// Register Key Vault with Aspire hosting extensions so secrets can be resolved


builder.Build().Run();
