using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.ServiceDiscovery;
namespace jarvis.Web;

public class IntegrationsApiClient(IConfiguration config)
{
    public async Task<string> GenerateAuthLink(CancellationToken cancellationToken = default)
    {

        var apiUrl = config["apiService"];


        var url = $"{apiUrl}/Integrations/GenerateAuthLink";
        return url;

    }
}

