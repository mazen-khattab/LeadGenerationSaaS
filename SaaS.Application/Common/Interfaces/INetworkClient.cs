using SaaS.Application.Common.Models;
using System.Threading;
using System.Threading.Tasks;

namespace SaaS.Application.Common.Interfaces
{
    /// <summary>
    /// Minimal abstraction for sending JSON payloads to external services.
    /// </summary>
    public interface INetworkClient
    {
        /// <summary>
        /// Posts the provided object as JSON to the specified url. Returns true when the response is a success 2xx.
        /// </summary>
        Task<NetworkResult> PostJsonAsync(string url, object payload, CancellationToken cancellationToken = default);
    }
}
