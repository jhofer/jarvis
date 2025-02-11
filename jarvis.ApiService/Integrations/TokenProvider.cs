using Azure.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PDFRenamer;
using PDFRenamer.Services;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jarvis.ApiService.Integrations
{

    public class TokenResponse
    {
        public string access_token { get; set; }
        public string refresh_token { get; set; }
    }

    public interface ITokenProvider
    {
        Task<string> GetAccessToken(string userId);

        public Task<TokenResponse> GetNewAccessToken(string refreshToken);
    }

    public class TokenProvider : ITokenProvider
    {
        private readonly ILogger logger;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IAccessRepository accessRepo;

        private readonly Dictionary<string, TokenResponse> tokenRepsonses = new Dictionary<string, TokenResponse>();

        public TokenProvider(ILogger<TokenProvider> logger, IHttpClientFactory httpClientFactory, IAccessRepository accessRepo)
        {
            this.logger = logger;
            this.httpClientFactory = httpClientFactory;
            this.accessRepo = accessRepo;
        }


        public JwtSecurityToken? ToToken(string accessToken)
        {
            var handler = new JwtSecurityTokenHandler();

            if (!handler.CanReadToken(accessToken))
            {
                return null;
            }

            var token = handler.ReadJwtToken(accessToken);
            return token;
        }

        public async Task<string> GetAccessToken(string userId)
        {

            string validToken = null;
            if (tokenRepsonses.TryGetValue(userId, out var tokens))
            {
                // chekc if token.access_token still valid;
                // if not, get new token with token.refresh_token

                var accessToken = ToToken(tokens.access_token);
                var refreshToken = tokens.refresh_token;
                if (accessToken?.ValidTo > DateTime.UtcNow.AddMinutes(1))
                {
                    logger.LogInformation("Access Token still valid");
                    validToken = tokens.access_token;
                }
                else
                {
                    logger.LogInformation("Access Token expired, refreshing token");
                    try
                    {

                        var newAccessToken = await GetNewAccessToken(tokens.refresh_token);
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

        public async Task<TokenResponse> GetNewAccessToken(string refreshToken)
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

            var repsonse = JsonConvert.DeserializeObject<TokenResponse>(responseBody);
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

