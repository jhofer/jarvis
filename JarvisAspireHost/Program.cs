
var builder = DistributedApplication.CreateBuilder(args);





var frontend =  builder.AddNpmApp("nextjs-frontend", "../nextjs-frontend", "dev")
    .WithNpmPackageInstallation()
    .WithHttpEndpoint(env: "PORT") 
    .WithExternalHttpEndpoints();

builder.Build().Run();
