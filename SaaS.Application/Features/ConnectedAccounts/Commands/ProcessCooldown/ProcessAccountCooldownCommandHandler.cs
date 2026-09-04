using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Common.Models;
using SaaS.Application.Common.Settings;
using SaaS.Domain.Enums;
using SaaS.Domain.Extensions;

namespace SaaS.Application.Features.ConnectedAccounts.Commands.ProcessCooldown
{
    public class ProcessAccountCooldownCommandHandler : IRequestHandler<ProcessAccountCooldownCommand, ApiResponse<int>>
    {
        private readonly IAppDbContext _context;
        private readonly IOptionsMonitor<GeneralSettings> _options;
        private readonly ILogger<ProcessAccountCooldownCommandHandler> _logger;

        public ProcessAccountCooldownCommandHandler(
            IAppDbContext context,
            IOptionsMonitor<GeneralSettings> options,
            ILogger<ProcessAccountCooldownCommandHandler> logger)
        {
            _context = context;
            _options = options;
            _logger = logger;
        }

        public async Task<ApiResponse<int>> Handle(ProcessAccountCooldownCommand request, CancellationToken cancellationToken)
        {
            var cooldownDays = _options.CurrentValue.AccountCooldownperiodDays;
            var targetDate = DateTime.UtcNow.AddDays(-cooldownDays);

            var coolingDownStatus = AccountStatus.COOLING_DOWN.ToDbString();

            // Fetch accounts in COOLING_DOWN state
            var accounts = await _context.ConnectedAccounts
                .Where(a => a.Status == coolingDownStatus)
                .ToListAsync(cancellationToken);

            if (accounts.Count == 0)
            {
                _logger.LogDebug("No accounts found in COOLING_DOWN state.");
                return ApiResponse<int>.Success(0, "No accounts to reactivate.");
            }

            int reactivatedCount = 0;
            var activeStatus = AccountStatus.ACTIVE.ToDbString();

            foreach (var account in accounts)
            {
                if (account.LastStatusUpdatedAt < targetDate)
                {
                    account.Status = activeStatus;
                    account.LastStatusUpdatedAt = DateTime.UtcNow;
                    reactivatedCount++;
                }
            }

            if (reactivatedCount > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Successfully reactivated {ReactivatedCount} account(s) from COOLING_DOWN state.", reactivatedCount);
            }

            return ApiResponse<int>.Success(reactivatedCount, $"Reactivated {reactivatedCount} account(s).");
        }
    }
}
