using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Domain.ExceptionTypes
{
    public class UnauthorizedException(string message) : Exception(message);
}
