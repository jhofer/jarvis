using Microsoft.Graph;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace PDFRenamerIsolated.Services.GraphClient
{
    public class CustomAuthenticationProvider : IAuthenticationProvider
    {
        private readonly string _accessToken;

        public CustomAuthenticationProvider(string accessToken)
        {
            _accessToken = accessToken;
        }



        public Task AuthenticateRequestAsync(RequestInformation request, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
        {
            // Füge den Access Token als Authorization-Header hinzu
            request.Headers.Add("Authorization", $"Bearer {_accessToken}");
            return Task.CompletedTask;
        }
    }
}
