using MediatR;
using SaaS.Application.Common.Interfaces;
using SaaS.Application.Common.Models;
using SaaS.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SaaS.Application.Features.Worker.Commands.LogBotActivity
{
    public class LogBotActivityCommandHandler : IRequestHandler<LogBotActivityCommand, ApiResponse<bool>>
    {
        private readonly IAppDbContext _db;

        public LogBotActivityCommandHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<ApiResponse<bool>> Handle(LogBotActivityCommand request, CancellationToken cancellationToken)
        {
            var log = new BotActivityLog
            {
                CorrelationId = request.CorrelationId,
                UserId = request.UserId,
                LogLevel = request.LogLevel,
                Message = request.Message,
                StackTrace = request.StackTrace,
                CreatedAt = DateTime.UtcNow
            };

            _db.BotActivityLogs.Add(log);
            await _db.SaveChangesAsync(cancellationToken);

            return ApiResponse<bool>.Success(true, "Bot activity logged successfully.");
        }
    }
}
