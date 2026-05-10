using BuildingBlocks.Security.EmailAuth;
using BuildingBlocks.Security.Encryption;
using BuildingBlocks.Security.JWT;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Security.Tests;

public class SecurityServiceRegistrationTests
{
    [Fact]
    public void AddBuildingBlocksSecurity_ShouldRegisterDefaultSecurityServices()
    {
        var services = new ServiceCollection();

        services.AddBuildingBlocksSecurity();
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(ITokenHelper) &&
            descriptor.ImplementationType == typeof(JwtHelper));

        using var provider = services.BuildServiceProvider();

        provider.GetService<IPasswordHasher>().Should().BeOfType<HmacSha512PasswordHasher>();
        provider.GetService<IEmailAuthService>().Should().NotBeNull();
    }

    [Fact]
    public void AddBuildingBlocksSecurity_ShouldHonorRegistrationOptions()
    {
        var services = new ServiceCollection();

        services.AddBuildingBlocksSecurity(options =>
        {
            options.SkipJwtRegistration = true;
            options.EnableEmailAuthentication = false;
        });
        using var provider = services.BuildServiceProvider();

        provider.GetService<ITokenHelper>().Should().BeNull();
        provider.GetService<IEmailAuthService>().Should().BeNull();
        provider.GetService<IPasswordHasher>().Should().BeOfType<HmacSha512PasswordHasher>();
    }
}
