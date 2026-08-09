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

        public static NetworkResult Ok(int statusCode) =>
            new() { IsSuccess = true, StatusCode = statusCode };

        public static NetworkResult Fail(int? statusCode, string errorMessage) =>
            new() { IsSuccess = false, StatusCode = statusCode, ErrorMessage = errorMessage };
    }

}
