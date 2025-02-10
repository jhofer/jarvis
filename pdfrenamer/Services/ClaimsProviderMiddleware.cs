using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace PDFRenamerIsolated.Services
{
    public class ClaimsProvider
    {
        public void SetClaims(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
            };
            var claims = handler.ValidateToken(token, validationParameters, out SecurityToken validToken);
            Claims = claims.Claims;
        }
        public IEnumerable<Claim> Claims { get; set; } = new List<Claim>();
        public string UserId => Claims.First(c => c.Type == "sub").Value;

    }


    internal class ClaimsProviderMiddleware : IFunctionsWorkerMiddleware
    {
        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)

        {
            var request = await context.GetHttpRequestDataAsync();
            var headers = request.Headers;
            if (!headers.Contains("Authorization"))
            {
                await next(context);
                return;
            }
            var authHeader = headers.First(h => h.Key == "Authorization").Value.FirstOrDefault();
            if (String.IsNullOrEmpty(authHeader))
            {
                await next(context);
                return;
            }

            var startWithBearer = authHeader.StartsWith("Bearer ", StringComparison.InvariantCulture);
            if (!startWithBearer)
            {
                await next(context);
                return;
            }
            var bearerToken = authHeader.Split(" ")[1];

            // validate token
            var handler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

            };

            var claims = handler.ValidateToken(bearerToken, validationParameters, out SecurityToken validToken);
            context.InstanceServices.GetService<ClaimsProvider>()!.Claims = claims.Claims;
            await next(context);
            return;
        }
    }
}
