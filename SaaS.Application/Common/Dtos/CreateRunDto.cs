using System;

namespace SaaS.Application.Common.Dtos
{
    public record CreateRunDto(int BotId, int ConnectedAccountId, int? TargetGroupId, string InfoJson);
}
