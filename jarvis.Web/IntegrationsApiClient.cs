using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.ServiceDiscovery;
using System.Net.Http.Headers;
using System.Net.Http;
using Microsoft.AspNetCore.Authentication;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Azure.Core;
using Microsoft.Identity.Web;
using System.Security.Claims;
using jarvis.DTOs;
namespace jarvis.Web;

public class IntegrationsApiClient
{
    private HttpClient client;
    private IConfiguration config;
    private IHttpContextAccessor httpContext;
    private ITokenAcquisition tokenAcquisition;

    public IntegrationsApiClient(IConfiguration config, HttpClient client, IHttpContextAccessor context, ITokenAcquisition tokenAcquisition)

    {
        client.BaseAddress = new("https+http://apiservice");
        this.client = client;
        this.config = config;
        this.httpContext = context;
        this.tokenAcquisition = tokenAcquisition;
    }

    public async Task<T> Get<T>(string url, CancellationToken cancellationToken = default)
    {
        return await SendRequest<T>(HttpMethod.Get, url, null, cancellationToken);
    }

    public async Task<T> Post<T>(string url, object body, CancellationToken cancellationToken = default)
    {
        return await SendRequest<T>(HttpMethod.Post, url, body, cancellationToken);
    }

    private async Task<T> SendRequest<T>(HttpMethod method, string url, object? body = default, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(method, url);
        if (body != null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(body));
        }
        await AddToken(request);
        var result = await client.SendAsync(request, cancellationToken);
        return await ExtractResponse<T>(url, result);
    }

    private async Task AddToken(HttpRequestMessage request)
    {
        var accessToken = await tokenAcquisition.GetAccessTokenForUserAsync(new[] { "api://0d6c8f5c-ba29-483e-9176-1f0bb9a50226/access_as_user" });

        if (string.IsNullOrEmpty(accessToken))
        {
            throw new InvalidOperationException("Access token is not available. Please ensure you are authenticated.");
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private static async Task<T> ExtractResponse<T>(string url, HttpResponseMessage result)
    {
        var responseBody = await result.Content.ReadAsStringAsync();
        if (result.IsSuccessStatusCode)
        {
            try
            {
                var obj = JsonSerializer.Deserialize<T>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return obj;
            }
            catch (Exception e)
            {
                throw new HttpRequestException($"Request to deserialize: {responseBody}");

            }

        }
        else
        {
            throw new HttpRequestException($"Request to {url} failed with status code {result.StatusCode}. Response: {responseBody}");
        }
    }

    public string GenerateAuthLink()
    {
        var ctx = httpContext.HttpContext! ?? throw new Exception("Http context not available");

        var id = ctx.User?.Identity ?? throw new Exception("User context not available");
        var userEmail = id.Name;
        var apiUrl = config["apiService"];

        var request = ctx.Request;
        var referer = request.Headers.Referer;
        var url = $"{apiUrl}/Integrations/GenerateAuthLink?referer={referer}";
        return url;

    }

    public async Task<IntegrationDTO[]> GetIntegrations()
    {
        var dtos = await this.Get<IEnumerable<IntegrationDTO>>("/integrations");
        return dtos.ToArray();
    }
}

