using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace BuildingBlocks.Messaging.MassTransit;

public static class Extensions
{
    public static IServiceCollection AddMessageBus(
        this IServiceCollection services, 
        IConfiguration configuration, 
        Assembly? assembly = null,
        Action<IBusRegistrationConfigurator>? configure = null)
    { 
        var rabbitMqOptions = configuration.GetSection("RabbitMQ").Get<RabbitMqOptions>()
            ?? new RabbitMqOptions();

        services.AddMassTransit(busConfigurator =>
        {
            if (assembly != null)
            {
                busConfigurator.AddConsumers(assembly);
            }

            // Dışarıdan gelen ek konfigürasyonları (Saga vb.) uygula
            configure?.Invoke(busConfigurator);

            busConfigurator.SetKebabCaseEndpointNameFormatter();

            busConfigurator.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqOptions.Host, rabbitMqOptions.VirtualHost, h =>
                {
                    h.Username(rabbitMqOptions.UserName);
                    h.Password(rabbitMqOptions.Password);
                });           
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
