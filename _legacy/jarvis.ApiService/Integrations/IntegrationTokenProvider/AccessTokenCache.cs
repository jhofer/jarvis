using jarvis.ApiService.Integrations.Registration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;

namespace jarvis.ApiService.Integrations.IntegrationTokenProvider
{
    public interface IAccessTokenCache
    {
        bool TryGetAccessToken(string userId, IntegrationType integraionType, out JwtSecurityToken? accessToken);

        public JwtSecurityToken AddToken(string userId, IntegrationType integrationType, string accessToken);
    }
    public class AccessTokenCache : IAccessTokenCache
    {
        private IDatabase cache;

        public AccessTokenCache(IConnectionMultiplexer connectionMux)
        {
            cache = connectionMux.GetDatabase();
        }

        private JwtSecurityToken ToToken(string accessToken)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(accessToken);
            return token;
        }
        public bool TryGetAccessToken(string userId, IntegrationType integraionType, out JwtSecurityToken? accessToken)
        {
            var key = GetKey(userId, integraionType);
            var token = cache.StringGet(key);
            if (token != RedisValue.Null)
            {
                var cachedToken = ToToken(token!);
                if (cachedToken.ValidTo > DateTime.UtcNow.AddMinutes(1))
                {
                    accessToken = cachedToken;
                    return true;

                }
            }
            accessToken = null;
            return false;
        }

        private string GetKey(string userId, IntegrationType integraionType)
        {
            return $"accesskey-{userId}-{integraionType}";
        }

        public JwtSecurityToken AddToken(string userId, IntegrationType integrationType, string accessToken)
        {
            cache.StringSet(GetKey(userId, integrationType), accessToken);
            return ToToken(accessToken);
        }
    }

}
