
var builder = DistributedApplication.CreateBuilder(args);

var acr = builder.AddAzureContainerRegistry("my-acr");

var kv = builder.AddAzureKeyVault("secrets");

var frontend = builder.AddDockerfile("nextjs-frontend", "../nextjs-frontend")
    .WithHttpEndpoint(3000)
    .WithExternalHttpEndpoints();


// Register Key Vault with Aspire hosting extensions so secrets can be resolved


builder.Build().Run();
