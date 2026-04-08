using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TonerTrack.Application.Common;
using TonerTrack.Application.Common.Interfaces;
using TonerTrack.Application.DomainEventHandlers;
using TonerTrack.Domain.Repositories;
using TonerTrack.Infrastructure.BackgroundServices;
using TonerTrack.Infrastructure.NinjaRmm;
using TonerTrack.Infrastructure.Persistence;
using TonerTrack.Infrastructure.Snmp;

namespace TonerTrack.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Persistence
        services.Configure<JsonPersistenceOptions>(
            configuration.GetSection(JsonPersistenceOptions.Section));

        services.AddSingleton<IPrinterRepository, JsonPrinterRepository>();

        // SNMP
        services.AddSingleton<ISnmpService, SharpSnmpService>();

        // NinjaRMM
        services.Configure<NinjaRmmOptions>(
            configuration.GetSection(NinjaRmmOptions.Section));

        services.Configure<NinjaRmmTicketOptions>(
            configuration.GetSection(NinjaRmmTicketOptions.Section));

        services.AddHttpClient<INinjaRmmService, NinjaRmmService>((sp, client) =>
        {
            var ninjaOpts = configuration
                .GetSection(NinjaRmmOptions.Section)
                .Get<NinjaRmmOptions>() ?? new NinjaRmmOptions();
            client.BaseAddress = new Uri(ninjaOpts.BaseUrl);
        });

        // Domain Event Dispatcher
        services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();

        // Background Polling
        services.Configure<PollingOptions>(
            configuration.GetSection(PollingOptions.Section));

        services.AddHostedService<PrinterPollingBackgroundService>();

        return services;
    }
}
