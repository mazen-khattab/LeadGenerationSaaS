using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Infrastructure.Hubs
{
    [Authorize]
    public class AppNotificationHub : Hub
    {
        private readonly ILogger<AppNotificationHub> _logger;

        public AppNotificationHub(ILogger<AppNotificationHub> logger)
        {
            _logger = logger;
        }

        public override Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            var connectionId = Context.ConnectionId;

            _logger.LogInformation("User {UserId} connected to SignalR Hub with Connection ID: {ConnectionId}", userId, connectionId);

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            var connectionId = Context.ConnectionId;

            if (exception != null)
            {
                _logger.LogWarning(exception, "User {UserId} disconnected with error from Connection ID: {ConnectionId}", userId, connectionId);
            }
            else
            {
                _logger.LogInformation("User {UserId} safely disconnected from Connection ID: {ConnectionId}", userId, connectionId);
            }

            return base.OnDisconnectedAsync(exception);
        }
    }
}
