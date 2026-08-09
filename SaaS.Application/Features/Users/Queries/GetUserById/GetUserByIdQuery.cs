using MediatR;
using SaaS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Application.Features.Users.Queries.GetUserById
{
    public record GetUserByIdQuery(Guid UserId) : IRequest<User?>;
}
