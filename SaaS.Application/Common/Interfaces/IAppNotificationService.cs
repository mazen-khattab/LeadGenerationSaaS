using System;
using System.Threading.Tasks;

namespace SaaS.Application.Common.Interfaces
{
    public interface IAppNotificationService
    {
        Task NotifyRunCompletedAsync(Guid userId, int runId, int leadsCount);
        Task NotifyLeadStatusUpdatedAsync(Guid userId, long leadId, string status);
        Task NotifyJobFailedAsync(Guid userId, long jobId, string message);
    }
}
