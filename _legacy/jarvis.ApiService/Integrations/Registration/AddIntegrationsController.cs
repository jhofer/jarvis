using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using StackExchange.Redis;
using jarvis.ApiService.Cache;
using jarvis.ApiService.Integrations.IntegrationTokenProvider;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;
using System.IdentityModel.Tokens.Jwt;
using Azure.Core;


namespace jarvis.ApiService.Integrations.Registration
{


    public record IntegrationRequest(string SessionId,
         string UserId,
         string CodeVerifier,
         string CodeChallange,
         string Referer
        )
    { }




    [ApiController]
    [Route("integrations")]
    public class AddIntegrationsController(ILogger<AddIntegrationsController> logger, IConfiguration configuration, IHttpClientFactory httpClientFactory, IConnectionMultiplexer connectionMux, IIntegrationRepository integrationRepository) : ControllerBase
    {

        private readonly IDatabase cache = connectionMux.GetDatabase();


        [HttpGet("GenerateAuthLink")]
        [Authorize]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public RedirectResult GenerateAuthLink([FromQuery] string referer)
        {
            var request = HttpContext.Request;
            /* var bearer = authorization.Split("Bearer ").First();
             var handler = new JwtSecurityTokenHandler();
             var token = handler.ReadJwtToken(bearer);*/
            var userId = User?.Identity?.Name ?? throw new Exception("No User Context");

            // logger.LogInformation(JsonSerializer.Serialize(request));
            var sessionId = Guid.NewGuid().ToString();
            string? clientId = configuration["CLIENT_ID"];
            if (clientId is null) throw new ArgumentNullException("Config CLIENT_ID is null");

            var codeVerifier = PkceHelper.GenerateCodeVerifier();

            string codeChallenge = PkceHelper.GenerateCodeChallenge(codeVerifier);

            var integrationRequest = new IntegrationRequest
            (
                SessionId: sessionId,
                CodeVerifier: codeVerifier,
                CodeChallange: codeChallenge,
                Referer: referer,
                UserId: userId
            );

            cache.SetObject(sessionId, integrationRequest);

            string redirectUri = GetRedirectUri(sessionId);

            string scope = "Files.ReadWrite offline_access";


            string authUrl = $"https://login.microsoftonline.com/common/oauth2/v2.0/authorize?" +
                             $"client_id={clientId}&response_type=code&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                             $"&response_mode=query&scope={Uri.EscapeDataString(scope)}" +
                             $"&code_challenge={Uri.EscapeDataString(codeChallenge)}&code_challenge_method=S256";


            // return redirect response

            var rediretResult = new RedirectResult(url: authUrl, permanent: true,
                            preserveMethod: true);

            return rediretResult;
        }
        private string GetRedirectUri(string sessionId)
        {
            var host = HttpContext.Request.Host;
            string hostUrl = $"{HttpContext.Request.Scheme}://{host.Host}:{host.Port}";
            string redirectUri = $"{hostUrl}/Integrations/ExchangeCodeForToken?sessionId={sessionId}";

            logger.LogInformation($"Redirect URI: {redirectUri}");
            return redirectUri;
        }

        [HttpGet("ExchangeCodeForToken")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<ActionResult> ExchangeCodeForToken(
         [FromQuery] string? code,
         [FromQuery] string? error,
         [FromQuery] string? error_description,
         [FromQuery] string sessionId
        )
        {


            if (error is not null && code is null)
            {
                return BadRequest(new { error, error_description });
            }

            string redirectUri = GetRedirectUri(sessionId);



            var httpClient = httpClientFactory.CreateClient("tokenEndpoint");

            string clientId = configuration["CLIENT_ID"];
            string clientSecret = configuration["CLIENT_SECRET"];
            var tokenEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/token";

            var request = cache.ObjectGetDelete<IntegrationRequest>(sessionId);

            var codeVerifier = request.CodeVerifier;
            var requestData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", redirectUri),
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code_verifier", codeVerifier)
            });
            var response = await httpClient.PostAsync(tokenEndpoint, requestData);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                dynamic tokenData = JsonSerializer.Deserialize<TokenResponse>(responseBody);
                string accessToken = tokenData.access_token;
                string refreshToken = tokenData.refresh_token;

                var integration = new Integration
                {
                    UserId = request.UserId,
                    AppId = clientId,
                    IntegrationName = "OneDrive",
                    RefreshToken = refreshToken
                };

                integrationRepository.Save(integration);

                var rediretResult = new RedirectResult(url: request.Referer, permanent: true,
                           preserveMethod: true);

                return rediretResult;
                //return new OkObjectResult("you now can move back to " + request.Referer + "/Integrations");

            }
            else
            {
                return BadRequest();

            }

        }
    }
}
