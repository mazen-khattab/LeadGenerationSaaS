using SaaS.Application.Common.Models;
using SaaS.Domain.Enums;
using System.Threading;
using System.Threading.Tasks;

namespace SaaS.Application.Common.Interfaces
{
    /// <summary>
    /// Minimal abstraction for sending JSON payloads to external services.
    /// </summary>
    public interface INetworkClient
    {
        Task<NetworkResult> PostJsonAsync(string endpoint, object payload, ExternalSystem targetSystem, CancellationToken cancellationToken = default);
        Task<NetworkResult> GetAsync(string endpoint, ExternalSystem targetSystem, CancellationToken cancellationToken = default);

    }
}
