using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Common.Models;
using SaaS.Application.Common.Settings;

namespace SaaS.Infrastructure.Services
{
    public class NetworkClient : INetworkClient
    {
        public const string NamedClient = "N8nClient";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly N8nSecurity _n8nSecurity;
        private readonly ILogger<NetworkClient> _logger;

        public NetworkClient(
            IHttpClientFactory httpClientFactory,
            IOptions<N8nSecurity> options,
            ILogger<NetworkClient> logger)
        {
            _httpClientFactory = httpClientFactory;
            _n8nSecurity = options.Value;
            _logger = logger;
        }

        public async Task<NetworkResult> PostJsonAsync(string url, object payload, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                _logger.LogWarning("PostJsonAsync called with an empty URL.");
                return NetworkResult.Fail(null, "URL cannot be empty.");
            }

            if (payload is null)
            {
                _logger.LogWarning("PostJsonAsync called with a null payload for {Url}", url);
                return NetworkResult.Fail(null, "Payload cannot be null.");
            }

            using var request = BuildRequest(url, payload);

            try
            {
                var client = _httpClientFactory.CreateClient(NamedClient);
                var response = await client.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return NetworkResult.Ok((int)response.StatusCode);
                }

                _logger.LogWarning(
                    "POST to {Url} returned non-success status {StatusCode}",
                    url, response.StatusCode);

                return NetworkResult.Fail((int)response.StatusCode, $"Webhook returned status {(int)response.StatusCode}.");
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "POST to {Url} timed out", url);
                return NetworkResult.Fail(null, "Request timed out.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "POST to {Url} failed due to a network/connection error", url);
                return NetworkResult.Fail(null, "Network error occurred while calling the webhook.");
            }
            catch (OperationCanceledException)
            {
                // Caller explicitly cancelled the request (e.g. client disconnected).
                // Let it propagate instead of masking it as a normal failure.
                throw;
            }
        }

        private HttpRequestMessage BuildRequest(string url, object payload)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            var secret = _n8nSecurity.AuthSecret;
            if (!string.IsNullOrWhiteSpace(secret))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secret);
            }

            return request;
        }
    }
}
