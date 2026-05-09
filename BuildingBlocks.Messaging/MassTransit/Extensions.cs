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
        return services.AddBuildingBlocksMessageBus(
            configuration,
            assembly,
            configure,
            "RabbitMQ");
    }

    public static IServiceCollection AddBuildingBlocksMessageBus(
        this IServiceCollection services,
        IConfiguration configuration,
        Assembly? assembly = null,
        Action<IBusRegistrationConfigurator>? configure = null,
        string sectionName = "BuildingBlocks:Messaging:RabbitMQ")
    {
        var section = GetSection(configuration, sectionName, "RabbitMQ");
        var rabbitMqOptions = section.Get<RabbitMqOptions>()
            ?? new RabbitMqOptions();

        services.AddMassTransit(busConfigurator =>
        {
            if (assembly != null)
            {
                busConfigurator.AddConsumers(assembly);
            }

            configure?.Invoke(busConfigurator);
            busConfigurator.SetKebabCaseEndpointNameFormatter();

            busConfigurator.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqOptions.Host, rabbitMqOptions.VirtualHost, host =>
                {
                    host.Username(rabbitMqOptions.UserName);
                    host.Password(rabbitMqOptions.Password);
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    private static IConfigurationSection GetSection(
        IConfiguration configuration,
        string preferredSectionName,
        string fallbackSectionName)
    {
        var preferred = configuration.GetSection(preferredSectionName);
        return preferred.Exists()
            ? preferred
            : configuration.GetSection(fallbackSectionName);
    }
}
