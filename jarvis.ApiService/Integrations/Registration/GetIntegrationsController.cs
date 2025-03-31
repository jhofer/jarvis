using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using Microsoft.AspNetCore.Authorization;
using jarvis.DTOs;


namespace jarvis.ApiService.Integrations.Registration
{
    [ApiController]
    public class GetIntegrationsController(ILogger<AddIntegrationsController> logger, IConfiguration configuration, IHttpClientFactory httpClientFactory, IConnectionMultiplexer connectionMux, IIntegrationRepository integrationRepository) : ControllerBase
    {


        [HttpGet("/integrations")]
        [Authorize]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<ActionResult> GetIntegrations()
        {

            var integerations = integrationRepository.GetIntegrations(User.Identity.ToString());

            var dtos = integerations.Select(integration => new IntegrationDTO()
            {
                Name = integration.IntegrationName,
                RefreshToken = integration.RefreshToken,
            });
            return Ok(integerations);
        }
    }

}
