using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Application.Common.Dtos
{
    public record DispatchMessagingDto(int BotId, int AccountId, List<long> LeadIds);
}
