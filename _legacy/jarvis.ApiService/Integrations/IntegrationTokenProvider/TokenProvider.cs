using jarvis.ApiService.Integrations.Registration;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

namespace jarvis.ApiService.Integrations.IntegrationTokenProvider
{

    public class TokenResponse
    {
        public string access_token { get; set; }
        public string refresh_token { get; set; }
    }

    public interface ITokenProvider
    {
        Task<JwtSecurityToken> GetAccessToken(string userId, IntegrationType integrationType);
    }

    public class TokenProvider : ITokenProvider
    {
        private readonly ILogger logger;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IIntegrationRepository integrationRepo;
        private readonly IAccessTokenCache accessTokenCache;


        public TokenProvider(ILogger<TokenProvider> logger, IHttpClientFactory httpClientFactory, IIntegrationRepository integrationRepo, IAccessTokenCache cache)
        {
            this.logger = logger;
            this.httpClientFactory = httpClientFactory;
            this.integrationRepo = integrationRepo;
            this.accessTokenCache = cache;
        }


        public async Task<JwtSecurityToken> GetAccessToken(string userId, IntegrationType integrationType)
        {
            if (accessTokenCache.TryGetAccessToken(userId, integrationType, out var accessToken))
            {
                return accessToken!;
            }
            else
            {
                return await GetNewAccessToken(userId, integrationType);
            }
        }

        private async Task<JwtSecurityToken> GetNewAccessToken(string userId, IntegrationType integrationType)
        {
            var integration = integrationRepo.GetIntegration(userId, integrationType);
            var refreshToken = integration.RefreshToken;
            var tokenResponse = await RetrieveAccessToken(userId, integrationType);
            integration.RefreshToken = tokenResponse.refresh_token;
            integrationRepo.Save(integration);

            var newAccessToken = tokenResponse.access_token;
            return accessTokenCache.AddToken(userId, integrationType, newAccessToken);

        }

        private async Task<TokenResponse> RetrieveAccessToken(string refreshToken, IntegrationType integrationType)
        {
            var tokenEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/token";
            var client = httpClientFactory.CreateClient("tokenEndpoint");

            var requestData = new FormUrlEncodedContent(new[]
            {
        new KeyValuePair<string, string>("client_id", Environment.GetEnvironmentVariable("CLIENT_ID")),
        new KeyValuePair<string, string>("client_secret", Environment.GetEnvironmentVariable("CLIENT_SECRET")),
        new KeyValuePair<string, string>("refresh_token", refreshToken),
        new KeyValuePair<string, string>("grant_type", "refresh_token")
    });

            var response = await client.PostAsync(tokenEndpoint, requestData);
            var responseBody = await response.Content.ReadAsStringAsync();

            var repsonse = JsonSerializer.Deserialize<TokenResponse>(responseBody);
            if (response == null)
            {
                throw new Exception("Failed to refresh token");
            }
            else
            {
                return repsonse;
            }
        }

    }
}

