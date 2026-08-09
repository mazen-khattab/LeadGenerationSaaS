using System;
using System.Threading.Tasks;

namespace SaaS.Application.Common.Interfaces
{
    public interface IAppNotificationService
    {
        Task NotifyRunCompletedAsync(Guid userId, int runId, int leadsCount);
    }
}
