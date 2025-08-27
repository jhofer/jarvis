using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using PDFRenamerIsolated;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using PDFRenamer.Services;
using PDFRenamerIsolated.Services;

namespace PDFRenamer
{
    public class AuthFunction

    {
        private readonly ILogger<AuthFunction> log;
        private readonly IAccessRepository accessRepo;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ClaimsProvider claimProvider;
        private readonly ITokenProvider tokenProvider;

        public AuthFunction(ILogger<AuthFunction> logger, IAccessRepository accessRepo, IHttpClientFactory httpClientFactory, ClaimsProvider claimProvider, ITokenProvider tokenProvider)

        {
            this.log = logger;
            this.accessRepo = accessRepo;
            this.httpClientFactory = httpClientFactory;
            this.claimProvider = claimProvider;
            this.tokenProvider = tokenProvider;
        }

        public static string codeVerifier;


        [Function("GenerateAuthLink")]
        public HttpResponseData GenerateAuthLink(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestData req)
        {
            string clientId = Environment.GetEnvironmentVariable("CLIENT_ID");

            codeVerifier = PkceHelper.GenerateCodeVerifier();
            string codeChallenge = PkceHelper.GenerateCodeChallenge(codeVerifier);


            string redirectUri = GetRedirectUri(req);

            string scope = "Files.ReadWrite offline_access";


            string authUrl = $"https://login.microsoftonline.com/common/oauth2/v2.0/authorize?" +
                             $"client_id={clientId}&response_type=code&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                             $"&response_mode=query&scope={Uri.EscapeDataString(scope)}" +
                             $"&code_challenge={Uri.EscapeDataString(codeChallenge)}&code_challenge_method=S256";


            // return redirect response
            var response = req.CreateResponse(HttpStatusCode.Moved);
            response.Headers.Add("Location", authUrl);
            // no cache header
            response.Headers.Add("Cache-Control", "no-store");

            return response;
        }

        private string GetRedirectUri(HttpRequestData req)
        {
            // Dynamische Redirect URI basierend auf der aktuellen Anfrage
            string hostUrl = $"{req.Url.Scheme}://{req.Url.Host}:{req.Url.Port}";
            string redirectUri = $"{hostUrl}/api/ExchangeCodeForToken";

            log.LogInformation($"Redirect URI: {redirectUri}");
            return redirectUri;
        }

        [Function("ExchangeCodeForToken")]
        public async Task<HttpResponseData> ExchangeCodeForToken(
     [FromQuery] DateRange range)
        {
            //http://localhost:7143/api/ExchangeCodeForToken?error=invalid_request&error_description=Proof%20Key%20for%20Code%20Exchange%20is%20required%20for%20cross-origin%20authorization%20code%20redemption.



            string redirectUri = GetRedirectUri(req);

            string code = req.Query["code"];
            if (string.IsNullOrEmpty(code))
            {
                var resp = req.CreateResponse();
                resp.StatusCode = HttpStatusCode.BadRequest;
                await resp.WriteStringAsync("Code missing");
                return resp;
            }


            var httpClient = httpClientFactory.CreateClient("tokenEndpoint");

            string clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
            string clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");
            var tokenEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/token";
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
