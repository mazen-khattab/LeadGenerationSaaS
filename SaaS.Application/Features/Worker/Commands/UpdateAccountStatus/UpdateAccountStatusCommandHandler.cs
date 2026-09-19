using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Common.Models;
using SaaS.Domain.Enums;
using SaaS.Domain.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SaaS.Application.Features.Worker.Commands.UpdateAccountStatus
{
    public class UpdateAccountStatusCommandHandler : IRequestHandler<UpdateAccountStatusCommand, ApiResponse<bool>>
    {
        private readonly IAppDbContext _context;
        private readonly ILogger<UpdateAccountStatusCommandHandler> _logger;

        public UpdateAccountStatusCommandHandler(
            IAppDbContext context,
            ILogger<UpdateAccountStatusCommandHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ApiResponse<bool>> Handle(UpdateAccountStatusCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating ConnectedAccount {AccountId} status to {Status}", request.AccountId, request.Status);

            var account = await _context.ConnectedAccounts
                .FirstOrDefaultAsync(a => a.Id == request.AccountId, cancellationToken);

            if (account == null)
            {
                _logger.LogWarning("ConnectedAccount {AccountId} not found.", request.AccountId);
                return ApiResponse<bool>.Failure("Account not found.", ErrorType.NotFound);
            }

            var validStatus = Enum.TryParse<AccountStatus>(request.Status, true, out var currentStatus);

            if (!validStatus)
            {
                _logger.LogWarning("Invalid AccountStatus '{Status}' found for ConnectedAccount {AccountId}.", request.Status, account.Id);
                return ApiResponse<bool>.Failure("Invalid account status.", ErrorType.ValidationError);
            }

            account.Status = currentStatus.ToDbString();
            account.LastStatusUpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("ConnectedAccount {AccountId} status successfully updated to {Status}.", account.Id, account.Status);

            return ApiResponse<bool>.Success(true, "Account status updated successfully.");
        }

        //private static bool TryParseAccountStatus(string input, out AccountStatus status)
        //{
        //    if (string.IsNullOrWhiteSpace(input))
        //    {
        //        status = default;
        //        return false;
        //    }

        //    if (Enum.TryParse<AccountStatus>(input, true, out status))
        //    {
        //        return true;
        //    }

        //    var normalized = input.Replace("_", "").Replace("-", "").Trim();
        //    foreach (AccountStatus val in Enum.GetValues<AccountStatus>())
        //    {
        //        if (string.Equals(val.ToString().Replace("_", ""), normalized, StringComparison.OrdinalIgnoreCase) ||
        //            string.Equals(val.ToDbString(), normalized, StringComparison.OrdinalIgnoreCase))
        //        {
        //            status = val;
        //            return true;
        //        }
        //    }

        //    status = default;
        //    return false;
        //}
    }
}
