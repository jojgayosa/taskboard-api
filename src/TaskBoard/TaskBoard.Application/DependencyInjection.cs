using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TaskBoard.Application.Common.Behaviors;

namespace TaskBoard.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // AutoMapper — scans this assembly for all Profile classes
            services.AddAutoMapper(cfg => cfg.AddMaps(typeof(DependencyInjection).Assembly));

            // MediatR — scans this assembly for all IRequestHandler implementations
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);

                // Register pipeline behaviors in order — logging runs first, then validation
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            });

            // FluentValidation — scans this assembly for all AbstractValidator<T> classes
            services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

            return services;
        }
    }
}
