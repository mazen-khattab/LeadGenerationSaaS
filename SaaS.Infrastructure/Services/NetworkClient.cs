using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Common.Models;
using SaaS.Application.Common.Settings;
using SaaS.Domain.Enums;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace SaaS.Infrastructure.Services
{
    public class NetworkClient : INetworkClient
    {
        public const string NamedClient = "ExternalServicesClient";

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IReadOnlyDictionary<ExternalSystem, IExternalSystemRequestStrategy> _externalSystemRequestStrategies;
        private readonly ILogger<NetworkClient> _logger;

        public NetworkClient(
            IHttpClientFactory httpClientFactory,
            IEnumerable<IExternalSystemRequestStrategy> externalSystemRequestStrategies,
            ILogger<NetworkClient> logger)
        {
            _httpClientFactory = httpClientFactory;
            _externalSystemRequestStrategies = externalSystemRequestStrategies.ToDictionary(d => d.System);
            _logger = logger;
        }

        public Task<NetworkResult> PostJsonAsync(string endpoint, object payload, ExternalSystem targetSystem, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
                return Task.FromResult(NetworkResult.Fail(null, "Endpoint cannot be null or empty."));

            if (payload is null)
                return Task.FromResult(NetworkResult.Fail(null, "Payload cannot be null."));

            var request = BuildRequest(HttpMethod.Post, endpoint, targetSystem);
            request.Content = JsonContent.Create(payload, options: JsonOptions);

            return ExecuteRequestAsync(request, cancellationToken);
        }

        public Task<NetworkResult> GetAsync(string endpoint, ExternalSystem targetSystem, CancellationToken cancellationToken = default)
        {
            var request = BuildRequest(HttpMethod.Get, endpoint, targetSystem);
            return ExecuteRequestAsync(request, cancellationToken);
        }

        private HttpRequestMessage BuildRequest(HttpMethod method, string endpoint, ExternalSystem targetSystem)
        {
            if (!_externalSystemRequestStrategies.TryGetValue(targetSystem, out var strategy))
                throw new NotSupportedException($"External system '{targetSystem}' has no registered request startegy.");

            var request = new HttpRequestMessage(method, BuildUri(strategy.ResolveBaseUrl(endpoint), endpoint));
            strategy.ApplyAuthentication(request);
            return request;
        }

        private static Uri BuildUri(string baseUrl, string endpoint)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                return new Uri(endpoint, UriKind.Absolute);

            return new Uri($"{baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}");
        }

        private async Task<NetworkResult> ExecuteRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient(NamedClient);
                using var response = await httpClient.SendAsync(request, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                    return NetworkResult.Ok((int)response.StatusCode, body);

                _logger.LogWarning(
                    "Request to {Url} returned non-success status {StatusCode}",
                    request.RequestUri, response.StatusCode);

                return NetworkResult.Fail((int)response.StatusCode, body);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Request to {Url} timed out", request.RequestUri);
                return NetworkResult.Fail(null, "Request timed out.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Request to {Url} failed due to a network/connection error", request.RequestUri);
                return NetworkResult.Fail(null, "Network error occurred.");
            }
            finally
            {
                // Disposed here (not via "using var request" in PostJsonAsync/GetAsync) because those
                // two methods are NOT async - they return this method's Task directly without awaiting it.
                // If the request were disposed in their scope, disposal would happen the instant the
                // synchronous method body finishes, likely before the HTTP call actually completes.
                request.Dispose();
            }
        }

    }
}
