using jarvis.ApiService.Integrations;
using System.Runtime.CompilerServices;

namespace jarvis.ApiService
{
    public static class ServiceRegistrations
    {
        public static void AddAppServices(this IServiceCollection services)
        {
            services
                .AddSingleton<IIntegrationRepository, IntegrationRepository>()
                .AddSingleton<ITokenProvider, TokenProvider>()
                .AddSingleton<IAccessTokenCache, AccessTokenCache>()
                .AddSingleton<>

            /*.AddSingleton<IOneDrive, OneDrive>()
            .AddSingleton<ICache, Cache>()
            .AddSingleton<IAI, AI>();*/

          /*  services
            .AddScoped<ClaimsProvider>();*/
        }
    }
}
