using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Domain.Enums
{
    public enum ErrorType
    {
        None, // => 200
        InvalidCredentials, // => 401
        Unauthorized, // => 401
        NotFound, // => 404
        InsufficientStock, // => 400
        BadRequest, // => 400
        ConcurrencyConflict, // => 409
        ValidationError, // => 422,
        TooManyRequests, // => 429
        ServerError, // => 500
    }
}
