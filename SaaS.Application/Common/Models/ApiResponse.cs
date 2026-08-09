using SaaS.Domain.Enums;
using System;

namespace SaaS.Application.Common.Models
{
    public class ApiResponse<T>
    {
        public bool IsSuccess { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public ErrorType ErrorType { get; set; } = ErrorType.None;
        public T? Data { get; set; }

        public static ApiResponse<T> Success(T? data, string message = "")
        {
            return new ApiResponse<T> { IsSuccess = true, Data = data, Message = message };
        }

        public static ApiResponse<T> Failure(string message, ErrorType errorType)
        {
            return new ApiResponse<T> { IsSuccess = false, Message = message, ErrorType = errorType };
        }
    }
}