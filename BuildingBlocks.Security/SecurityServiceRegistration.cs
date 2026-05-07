using BuildingBlocks.Security.EmailAuth;
using BuildingBlocks.Security.Encryption;
using BuildingBlocks.Security.JWT;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Security;

public static class SecurityServiceRegistration
{
    /// <summary>
    /// Temel güvenlik servislerini ekler.
    /// </summary>
    public static IServiceCollection AddSecurityServices(
        this IServiceCollection services,
        Action<SecurityOptions>? configure = null)
    {
        var options = new SecurityOptions();
        configure?.Invoke(options);

        // 1. JWT Token Helper
        // NOT: AddJwtAuthentication() da register eder. Duplicating'den kaçınmak için SkipJwtRegistration kullan.
        if (!options.SkipJwtRegistration)
            services.AddScoped<ITokenHelper, JwtHelper>();

        // 2. Password Hasher
        services.AddScoped<IPasswordHasher, HmacSha512PasswordHasher>();

        // 3. Email Authentication
        if (options.EnableEmailAuthentication)
            services.AddScoped<IEmailAuthService, EmailAuthService>();

        // 4. OTP Authentication (gelecekte)
        // if (options.EnableOtpAuthentication)
        //     services.AddScoped<IOtpAuthenticatorHelper, OtpNetOtpAuthenticatorHelper>();

        // 5. Two-Factor Authentication (gelecekte)
        // if (options.EnableTwoFactorAuthentication)
        //     services.AddScoped<ITwoFactorService, TwoFactorService>();

        return services;
    }
}

/// <summary>
/// Güvenlik servislerini yapılandıran options sınıfı.
/// </summary>
public class SecurityOptions
{
    /// <summary>ITokenHelper kaydını atla (AddJwtAuthentication zaten register ediyorsa true yap)</summary>
    public bool SkipJwtRegistration { get; set; } = false;

    /// <summary>Email authentication (token üretme/doğrulama) aktif mi?</summary>
    public bool EnableEmailAuthentication { get; set; } = true;

    /// <summary>OTP authentication (Google Authenticator) aktif mi?</summary>
    public bool EnableOtpAuthentication { get; set; } = false;

    /// <summary>Two-factor authentication aktif mi?</summary>
    public bool EnableTwoFactorAuthentication { get; set; } = false;
}