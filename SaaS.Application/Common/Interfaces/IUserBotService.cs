using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Application.Common.Interfaces
{
    public interface IUserBotService
    {
        Task<bool> OwnerShipCheck(Guid userId, int botId, CancellationToken cancellationToken);
    }
}
