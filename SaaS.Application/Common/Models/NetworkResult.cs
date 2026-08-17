using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Application.Common.Models
{
    public sealed record NetworkResult
    {
        public bool IsSuccess { get; init; }
        public int? StatusCode { get; init; }
        public string? ErrorMessage { get; init; }
        public string? Content { get; init; }

        public static NetworkResult Ok(int statusCode, string? content = null) =>
            new() { IsSuccess = true, StatusCode = statusCode, Content = content };

        public static NetworkResult Fail(int? statusCode, string errorMessage) =>
            new() { IsSuccess = false, StatusCode = statusCode, ErrorMessage = errorMessage };
    }
}
