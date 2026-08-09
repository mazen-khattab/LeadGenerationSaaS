using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Application.Common.Dtos
{
    public record LoginRequestDto(string Email, string Password);
}
