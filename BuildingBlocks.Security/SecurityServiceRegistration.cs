using BuildingBlocks.Security.EmailAuth;
using BuildingBlocks.Security.Encryption;
using BuildingBlocks.Security.JWT;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Security;

public static class SecurityServiceRegistration
{
    public static IServiceCollection AddSecurityServices(
        this IServiceCollection services,
        Action<SecurityOptions>? configure = null)
    {
        return services.AddBuildingBlocksSecurity(configure);
    }

    public static IServiceCollection AddBuildingBlocksSecurity(
        this IServiceCollection services,
        Action<SecurityOptions>? configure = null)
    {
        var options = new SecurityOptions();
        configure?.Invoke(options);

        if (!options.SkipJwtRegistration)
        {
            services.AddScoped<ITokenHelper, JwtHelper>();
        }

        services.AddScoped<IPasswordHasher, HmacSha512PasswordHasher>();

        if (options.EnableEmailAuthentication)
        {
            services.AddScoped<IEmailAuthService, EmailAuthService>();
        }

        return services;
    }
}

public class SecurityOptions
{
    public bool SkipJwtRegistration { get; set; } = false;
    public bool EnableEmailAuthentication { get; set; } = true;
    public bool EnableOtpAuthentication { get; set; } = false;
    public bool EnableTwoFactorAuthentication { get; set; } = false;
}
