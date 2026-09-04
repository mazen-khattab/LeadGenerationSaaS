using SaaS.Domain.Enums;

namespace SaaS.Api.Extensions
{
    public static class ErrorTypeExtensions
    {
        public static int ToHttpStatusCode(this ErrorType errorType)
        {
            return errorType switch
            {
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.InvalidCredentials => StatusCodes.Status401Unauthorized,
                ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                ErrorType.ConcurrencyConflict => StatusCodes.Status409Conflict,
                ErrorType.InsufficientStock => StatusCodes.Status400BadRequest,
                ErrorType.BadRequest => StatusCodes.Status400BadRequest,
                ErrorType.ValidationError => StatusCodes.Status422UnprocessableEntity,
                ErrorType.TooManyRequests => StatusCodes.Status429TooManyRequests,
                ErrorType.None => StatusCodes.Status200OK,
                ErrorType.ServerError => StatusCodes.Status500InternalServerError,
                _ => StatusCodes.Status500InternalServerError
            };
        }
    }

}
