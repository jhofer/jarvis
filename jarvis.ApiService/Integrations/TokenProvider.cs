using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

namespace jarvis.ApiService.Integrations
{

    public class TokenResponse
    {
        public string access_token { get; set; }
        public string refresh_token { get; set; }
    }

    public interface ITokenProvider
    {
        Task<string> GetAccessToken(string userId, IntegrationType integrationType);


    }

    public class TokenProvider : ITokenProvider
    {
        private readonly ILogger logger;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IIntegrationRepository accessRepo;
        private readonly IAccessTokenCache cache;


        public TokenProvider(ILogger<TokenProvider> logger, IHttpClientFactory httpClientFactory, IIntegrationRepository accessRepo, IAccessTokenCache cache)
        {
            this.logger = logger;
            this.httpClientFactory = httpClientFactory;
            this.accessRepo = accessRepo;
            this.cache = cache;
        }


        private JwtSecurityToken? ToToken(string accessToken)
        {
            var handler = new JwtSecurityTokenHandler();

            if (!handler.CanReadToken(accessToken))
            {
                return null;
            }

            var token = handler.ReadJwtToken(accessToken);
            return token;
        }

        public async Task<string> GetAccessToken(string userId, IntegrationType integrationType)
        {

            string validToken = null;

            if (cache.TryGetAccessToken(userId, integrationType, out var accessToken))
            {
                if (accessRepo.TryGetRefreshToken(userId, out string refreshToken))
                {
                    var newAccessToken = await GetNewAccessToken(userId, integrationType);

                }
                else
                {

                }
              


              
                var refreshToken = newAccessToken.refresh_token;
                var accessToken = newAccessToken.access_token;


                accessRepo.Update(userId, integrationType, refreshToken);

                tokenRepsonses[userId] = newAccessToken;
                //accessRepo.SaveRefreshToken(userId, newAccessToken.refresh_token);
                validToken = newAccessToken.access_token;



            }
            else if (accessRepo.TryGetRefreshToken(userId, out string refreshToken))
            {


                logger.LogInformation("Access Token expired, refreshing token");
                try
                {

                    var newAccessToken = await GetNewAccessToken(refreshToken);
                    tokenRepsonses[userId] = newAccessToken;
                    accessRepo.SaveRefreshToken(userId, newAccessToken.refresh_token);
                    validToken = newAccessToken.access_token;
                }
                catch (Exception e)
                {
                    logger.LogError("Refresh Token expired");
                    throw;
                }
            }



            if (validToken != null)
            {
                return validToken;
            }
            else
            {
                throw new Exception("No token found for user");
            }
        }

        public async Task<TokenResponse> GetNewAccessToken(string refreshToken, IntegrationType integrationType)
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

