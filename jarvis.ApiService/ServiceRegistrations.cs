using jarvis.ApiService.Integrations;
using System.Runtime.CompilerServices;

namespace jarvis.ApiService
{
    public static class ServiceRegistrations
    {
        public static void AddAppServices(this IServiceCollection services)
        {
            services
                .AddSingleton<IAccessRepository, StorageAccessRepository>()
                .AddSingleton<ITokenProvider, TokenProvider>();
            /*.AddSingleton<IOneDrive, OneDrive>()
            .AddSingleton<ICache, Cache>()
            .AddSingleton<IAI, AI>();*/

          /*  services
            .AddScoped<ClaimsProvider>();*/
        }
    }
}
