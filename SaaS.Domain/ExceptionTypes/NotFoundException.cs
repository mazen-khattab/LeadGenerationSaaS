using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Domain.ExceptionTypes
{
    public class NotFoundException(string message) : Exception(message);
}
