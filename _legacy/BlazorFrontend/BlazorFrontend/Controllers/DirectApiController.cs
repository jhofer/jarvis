using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;

namespace BlazorAzureADWithApis.Server.Controllers;

[Authorize(Policy = "ValidateAccessTokenPolicy",
    AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[AuthorizeForScopes(Scopes = new string[] { "api://0d6c8f5c-ba29-483e-9176-1f0bb9a50226/access_as_user" })]
[ApiController]
[Route("[controller]")]
public class DirectApiController : ControllerBase
{
    [HttpGet]
    public IEnumerable<string> Get()
    {
        return new List<string> { "some data", "more data", "loads of data" };
    }
}