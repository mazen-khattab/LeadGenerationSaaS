using MediatR;
using Microsoft.EntityFrameworkCore;
using SaaS.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Application.Common.Services
{
    public class UserBotService : IUserBotService
    {
        private readonly IAppDbContext _dbContext;

        public UserBotService(IAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> OwnerShipCheck(Guid userId, int botId, CancellationToken cancellationToken) 
            => await _dbContext.UserBots
                .AsNoTracking()
                .AnyAsync(
                    ub => ub.UserId == userId &&
                          ub.BotId == botId,
                    cancellationToken);
        
    }
}
