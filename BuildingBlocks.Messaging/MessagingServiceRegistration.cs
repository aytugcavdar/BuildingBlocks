using BuildingBlocks.Messaging.Email;
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
    /// Email, template ve mesaj bus servislerini register eder.
    /// </summary>
    public static IServiceCollection AddMessagingServices(
        this IServiceCollection services,
        IConfiguration configuration,
        Assembly? consumersAssembly = null)
    {
        // EmailOptions konfigürasyonu
        services.Configure<EmailOptions>(configuration.GetSection("EmailOptions"));

        // Template servisi
        services.AddScoped<ITemplateService, TemplateService>();

        // SMTP Email servisi
        services.AddScoped<IEmailService, SmtpEmailService>();

        // RabbitMQ / MassTransit
        services.AddMessageBus(configuration, consumersAssembly);

        return services;
    }

    /// <summary>
    /// SMS servisini register eder.
    /// Production'da gerçek ISmsService implementasyonu ile override et.
    /// </summary>
    public static IServiceCollection AddSmsServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Development/debug için stub implementation
        services.AddScoped<ISmsService, DebugSmsService>();
        return services;
    }
}
