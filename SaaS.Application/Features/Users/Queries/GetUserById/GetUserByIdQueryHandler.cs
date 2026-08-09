using MediatR;
using Microsoft.EntityFrameworkCore;
using SaaS.Application.Common.Interfaces;
using SaaS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Application.Features.Users.Queries.GetUserById
{
    public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, User?>
    {
        private readonly IAppDbContext _context;

        public GetUserByIdQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            return await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == request.UserId && !u.IsDeleted, cancellationToken);
        }
    }
}
