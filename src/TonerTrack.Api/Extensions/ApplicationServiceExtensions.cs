using FluentValidation;
using MediatR;
using TonerTrack.Application.Common.Behaviors;
using TonerTrack.Application.DomainEventHandlers;

namespace TonerTrack.Api.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var appAssembly = typeof(TonerLowTicketHandler).Assembly;

        // Register all IRequestHandler<,> and INotificationHandler<> implementations
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(appAssembly));

        // Register all AbstractValidator<T> implementations from the Application assembly
        services.AddValidatorsFromAssembly(appAssembly);

        // Plug validation into the MediatR pipeline
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
