using MediatR;
using SaaS.Application.Common.Models;
using System;
using System.Net.NetworkInformation;
using System.Text.Json.Serialization;

namespace SaaS.Application.Features.Worker.Commands.UpdateLeadStatus
{
    public record UpdateLeadStatusCommand(long LeadId, string Status) : IRequest<ApiResponse<bool>>;
}
