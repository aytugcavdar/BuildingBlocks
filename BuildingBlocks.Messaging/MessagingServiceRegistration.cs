using BuildingBlocks.Messaging.Email;
using BuildingBlocks.Messaging.EventBus;
using BuildingBlocks.Messaging.MassTransit;
using BuildingBlocks.Messaging.SMS;
using BuildingBlocks.Messaging.Templates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace BuildingBlocks.Messaging;

public static class MessagingServiceRegistration
{
    /// <summary>
    /// Registers generic messaging, email, and template services.
    /// </summary>
    public static IServiceCollection AddMessagingServices(
        this IServiceCollection services,
        IConfiguration configuration,
        Assembly? consumersAssembly = null)
    {
        return services.AddBuildingBlocksMessaging(
            configuration,
            consumersAssembly,
            emailSectionName: "EmailOptions",
            rabbitMqSectionName: "RabbitMQ");
    }

    /// <summary>
    /// Registers BuildingBlocks messaging, email, and template services.
    /// Reads "BuildingBlocks:Messaging:*" sections by default and falls back to legacy names.
    /// </summary>
    public static IServiceCollection AddBuildingBlocksMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        Assembly? consumersAssembly = null,
        string emailSectionName = "BuildingBlocks:Messaging:Email",
        string rabbitMqSectionName = "BuildingBlocks:Messaging:RabbitMQ")
    {
        var emailSection = GetSection(configuration, emailSectionName, "EmailOptions");
        services.Configure<EmailOptions>(emailSection);

        services.AddScoped<ITemplateService, TemplateService>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<IEventBus, MassTransitEventBus>();
        services.AddBuildingBlocksMessageBus(
            configuration,
            consumersAssembly,
            sectionName: rabbitMqSectionName);

        return services;
    }

    /// <summary>
    /// Registers the default SMS service. Override ISmsService in production when needed.
    /// </summary>
    public static IServiceCollection AddSmsServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddBuildingBlocksSms(configuration);
    }

    public static IServiceCollection AddBuildingBlocksSms(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<ISmsService, DebugSmsService>();
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
