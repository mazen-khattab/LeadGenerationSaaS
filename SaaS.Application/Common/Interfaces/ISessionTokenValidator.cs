using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Application.Common.Interfaces
{
    public interface ISessionTokenValidator
    {
        Task<string?> GetCurrentSessionTokenAsync(Guid userId);
    }
}
