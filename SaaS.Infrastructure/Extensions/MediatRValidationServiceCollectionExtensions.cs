using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SaaS.Application.Behaviors;
using SaaS.Application.Features.Auth.Commands.User.Login;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SaaS.Infrastructure.Extensions
{
    public static class MediatRValidationServiceCollectionExtensions
    {
        public static IServiceCollection AddMediatRWithValidation(this IServiceCollection services)
        {
            var assembly = typeof(UserLoginCommandValidator).Assembly;

            services.AddMediatR(config =>
            {
                config.RegisterServicesFromAssembly(assembly);
                config.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            });

            services.AddValidatorsFromAssembly(assembly);

            return services;
        }
    }
}
