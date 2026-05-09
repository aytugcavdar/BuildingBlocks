using BuildingBlocks.Security.Encryption;
using BuildingBlocks.Security.JWT;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;

namespace BuildingBlocks.CrossCutting.Authentication;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddBuildingBlocksJwtAuthentication(configuration, "TokenOptions");
    }

    public static IServiceCollection AddBuildingBlocksJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "BuildingBlocks:Security:TokenOptions")
    {
        var tokenOptions = configuration
            .GetSection(sectionName)
            .Get<TokenOptions>()
            ?? configuration
                .GetSection("TokenOptions")
                .Get<TokenOptions>()
            ?? throw new InvalidOperationException(
                "Token options configuration is missing. " +
                "Please add 'BuildingBlocks:Security:TokenOptions' or legacy 'TokenOptions' section.");

        ValidateSecurityKey(tokenOptions.SecurityKey);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = tokenOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = tokenOptions.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = SecurityKeyHelper.CreateSecurityKey(tokenOptions.SecurityKey),
                NameClaimType = System.Security.Claims.ClaimTypes.Name,
                RoleClaimType = System.Security.Claims.ClaimTypes.Role
            };
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception is SecurityTokenExpiredException)
                    {
                        context.Response.Headers.Append("Token-Expired", "true");
                    }

                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";

                    var result = JsonSerializer.Serialize(new
                    {
                        success = false,
                        message = "You are not authorized to access this resource. Please login.",
                        statusCode = StatusCodes.Status401Unauthorized
                    });

                    return context.Response.WriteAsync(result);
                },
                OnForbidden = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";

                    var result = JsonSerializer.Serialize(new
                    {
                        success = false,
                        message = "You do not have permission to access this resource.",
                        statusCode = StatusCodes.Status403Forbidden
                    });

                    return context.Response.WriteAsync(result);
                },
                OnTokenValidated = _ => Task.CompletedTask
            };
        });

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = null;
            AuthorizationPolicies.Register(options);
        });

        services.AddScoped<ITokenHelper, JwtHelper>();

        return services;
    }

    private static void ValidateSecurityKey(string securityKey)
    {
        if (string.IsNullOrWhiteSpace(securityKey))
        {
            throw new InvalidOperationException(
                "JWT SecurityKey cannot be empty. Set it via configuration or environment variable.");
        }

        if (securityKey.Contains("REPLACED", StringComparison.OrdinalIgnoreCase) ||
            securityKey.Contains("your_", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "JWT SecurityKey contains placeholder text. Set a real key.");
        }

        if (securityKey.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT SecurityKey must be at least 32 characters.");
        }
    }
}
