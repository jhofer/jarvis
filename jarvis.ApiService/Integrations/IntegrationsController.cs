using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net;

namespace jarvis.ApiService.Integrations
{

    public class Integration
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string RefreshToken { get; set; }
    }


    [ApiController]
    [Route("[controller]")]
    public class IntegrationsController : ControllerBase
    {

        private readonly ILogger<IntegrationsController> logger;
        private readonly IConfiguration configuration;
        private readonly IHttpClientFactory httpClientFactory;
        private string codeVerifier;

        public IntegrationsController(ILogger<IntegrationsController> logger, IConfiguration configuration, IHttpClientFactory httpClienFactory)
        {
            this.logger = logger;
            this.configuration = configuration;
            this.httpClientFactory = httpClienFactory;
        }

        [HttpGet("GenerateAuthLink")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public RedirectResult GenerateAuthLink()
        {

            string? clientId = this.configuration["CLIENT_ID"];
            if (clientId is null) throw new ArgumentNullException("Config CLIENT_ID is null");

            codeVerifier = PkceHelper.GenerateCodeVerifier();
            string codeChallenge = PkceHelper.GenerateCodeChallenge(codeVerifier);


            string redirectUri = GetRedirectUri();

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
        private string GetRedirectUri()
        {
            var host = HttpContext.Request.Host;
            string hostUrl = $"{HttpContext.Request.Scheme}://{host.Host}:{host.Port}";
            string redirectUri = $"{hostUrl}/api/ExchangeCodeForToken";

            logger.LogInformation($"Redirect URI: {redirectUri}");
            return redirectUri;
        }
        [HttpGet("ExchangeCodeForToken")]
        public async Task<ActionResult> ExchangeCodeForToken(
[FromQuery] string? code, [FromQuery] string? error, [FromQuery] string? error_description)
        {
            //http://localhost:7143/api/ExchangeCodeForToken?error=invalid_request&error_description=Proof%20Key%20for%20Code%20Exchange%20is%20required%20for%20cross-origin%20authorization%20code%20redemption.

            if (error is not null && code is null)
            {
                return BadRequest(new { error, error_description });
            }

            // string redirectUri = GetRedirectUri(req);



            var httpClient = httpClientFactory.CreateClient("tokenEndpoint");

            string clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
            string clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");
            var tokenEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/token";
            var requestData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("code", code),
                /*new KeyValuePair<string, string>("redirect_uri", redirectUri),*/
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code_verifier", codeVerifier)
            });
            var response = await httpClient.PostAsync(tokenEndpoint, requestData);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                dynamic tokenData = JsonConvert.DeserializeObject<TokenResponse>(responseBody);
                string accessToken = tokenData.access_token;
                string refreshToken = tokenData.refresh_token;
                var bearerToken = await tokenProvider.GetNewAccessToken(refreshToken);
                claimProvider.SetClaims(bearerToken.access_token);
                string userId = claimProvider.UserId;

                accessRepo.SaveRefreshToken(userId, refreshToken);
                // Speichere Refresh Token sicher in Azure Key Vault oder einer Datenbank.
                var newAccessToken = await tokenProvider.GetAccessToken(userId);

                var resp = req.CreateResponse();
                resp.StatusCode = HttpStatusCode.OK;
                await resp.WriteStringAsync(newAccessToken);
                return resp;

            }
            else
            {
                var resp = req.CreateResponse();
                resp.StatusCode = HttpStatusCode.BadRequest;
                await resp.WriteStringAsync(responseBody);
                return resp;

            }

        }


    }

}
