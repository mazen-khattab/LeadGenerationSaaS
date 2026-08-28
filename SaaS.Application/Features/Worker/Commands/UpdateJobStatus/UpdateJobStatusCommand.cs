using MediatR;
using SaaS.Application.Common.Models;
using System;
using System.Text.Json.Serialization;

namespace SaaS.Application.Features.Worker.Commands.UpdateJobStatus
{
    public record UpdateJobStatusCommand(long JobId, string Status) : IRequest<ApiResponse<bool>>;
}
