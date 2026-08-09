using Microsoft.AspNetCore.SignalR;
using SaaS.Application.Common.Interfaces;
using SaaS.Infrastructure.Hubs;
using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Infrastructure.Services
{
    public class SignalRNotificationService : IAppNotificationService
    {
        private readonly IHubContext<AppNotificationHub> _hubContext;
        public SignalRNotificationService(IHubContext<AppNotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }
        public async Task NotifyRunCompletedAsync(Guid userId, int runId, int leadsCount)
        {
            await _hubContext.Clients.User(userId.ToString())
                .SendAsync("RunCompleted", new
                {
                    RunId = runId,
                    LeadsCount = leadsCount
                });
        }
    }
}
