using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

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

        private readonly ILogger<IntegrationsController> _logger;
        private readonly IConfiguration configuration;
        private string codeVerifier;

        public IntegrationsController(ILogger<IntegrationsController> logger, IConfiguration configuration)
        {
            _logger = logger;
            this.configuration = configuration;
        }

        [HttpGet("GenerateAuthLink")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public RedirectResult GenerateAuthLink()
        {

            string clientId = this.configuration["CLIENT_ID"];

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

            _logger.LogInformation($"Redirect URI: {redirectUri}");
            return redirectUri;
        }


    }

}
