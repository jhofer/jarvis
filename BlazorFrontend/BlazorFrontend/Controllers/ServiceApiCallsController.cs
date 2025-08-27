using BlazorAzureADWithApis.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;

namespace BlazorAzureADWithApis.Server.Controllers;

[Authorize(Policy = "ValidateAccessTokenPolicy", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[AuthorizeForScopes(Scopes = new string[] { "api://0d6c8f5c-ba29-483e-9176-1f0bb9a50226/access_as_user" })]
[ApiController]
[Route("[controller]")]
public class ServiceApiCallsController : ControllerBase
{
    private readonly ServiceApiClientService _serviceApiClientService;

    public ServiceApiCallsController(ServiceApiClientService serviceApiClientService)
    {
        _serviceApiClientService = serviceApiClientService;
    }

    [HttpGet]
    public async Task<IEnumerable<string>?> Get()
    {
        return await _serviceApiClientService.GetApiDataAsync();
    }
}