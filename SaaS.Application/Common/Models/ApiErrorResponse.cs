using System;

namespace SaaS.Application.Common.Models
{
    public class ApiErrorResponse
    {
        public bool IsSuccess { get; set; } = false;
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string TraceId { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string? InnerException { get; set; }
    }
}