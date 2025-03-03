using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net;
using System.Text.Json;
using StackExchange.Redis;
using System.Text.Json.Serialization;
using jarvis.ApiService.Cache;


namespace jarvis.ApiService.Integrations
{


    public record IntegrationRequest(string SessionId,
         string UserId,
         string CodeVerifier,
         string CodeChallange,
         string Referer
        )
    { }




    [ApiController]
    [Route("[controller]")]
    public class IntegrationsController : ControllerBase
    {

        private readonly ILogger<IntegrationsController> logger;
        private readonly IConfiguration configuration;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IConnectionMultiplexer connectionMultiplexer;
        private readonly IDatabase cache;
        private readonly IIntegrationRepository integrationRepository;

        public IntegrationsController(ILogger<IntegrationsController> logger, IConfiguration configuration, IHttpClientFactory httpClienFactory, IConnectionMultiplexer connectionMux, IIntegrationRepository integrationRepository)
        {
            this.logger = logger;
            this.configuration = configuration;
            this.httpClientFactory = httpClienFactory;
            this.connectionMultiplexer = connectionMux;
            this.cache = connectionMultiplexer.GetDatabase();
            this.integrationRepository = integrationRepository;
        }

        [HttpGet("GenerateAuthLink")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public RedirectResult GenerateAuthLink([FromHeader] string referer, [FromQuery] string userEmail)
        {
            var sessionId = Guid.NewGuid().ToString();
            string? clientId = this.configuration["CLIENT_ID"];
            if (clientId is null) throw new ArgumentNullException("Config CLIENT_ID is null");

            var codeVerifier = PkceHelper.GenerateCodeVerifier();

            string codeChallenge = PkceHelper.GenerateCodeChallenge(codeVerifier);

            var integrationRequest = new IntegrationRequest
            (
                SessionId: sessionId,
                CodeVerifier: codeVerifier,
                CodeChallange: codeChallenge,
                Referer: referer,
                UserId: userEmail
            );

            this.cache.SetObject(sessionId, integrationRequest);

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

            string clientId = this.configuration["CLIENT_ID"];
            string clientSecret = this.configuration["CLIENT_SECRET"];
            var tokenEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/token";

            var request = this.cache.ObjectGetDelete<IntegrationRequest>(sessionId);

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

                var rediretResult = new RedirectResult(url: request.Referer + "/Integrations", permanent: true,
                           preserveMethod: true);

                return rediretResult;

            }
            else
            {
                return BadRequest();

            }

        }

        [HttpGet("/")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<ActionResult> GetIntegrations()
        {
            var integerations = integrationRepository.GetIntegrations(request)

        }




    }

}
