using BuildingBlocks.Security.Encryption;
using BuildingBlocks.Security.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace BuildingBlocks.Security.JWT;

/// <summary>
/// ITokenHelper'ın JWT implementasyonu.
/// Access token oluşturma, refresh token ve token doğrulama içerir.
/// </summary>
public class JwtHelper : ITokenHelper
{
    private readonly TokenOptions _tokenOptions;
    private readonly SecurityKey _securityKey;

    public JwtHelper(IConfiguration configuration)
    {
        _tokenOptions = configuration
            .GetSection("BuildingBlocks:Security:TokenOptions")
            .Get<TokenOptions>()
            ?? configuration
                .GetSection("TokenOptions")
                .Get<TokenOptions>()
            ?? throw new InvalidOperationException(
                "Token options configuration is missing. " +
                "Please add 'BuildingBlocks:Security:TokenOptions' or legacy 'TokenOptions' section.");

        _securityKey = SecurityKeyHelper.CreateSecurityKey(_tokenOptions.SecurityKey);
    }

    public AccessToken CreateToken(Guid userId, string email, string userName, List<string> roles)
    {
        var expiration = DateTime.UtcNow.AddMinutes(_tokenOptions.AccessTokenExpiration);
        var signingCredentials = SigningCredentialsHelper.CreateSigningCredentials(_securityKey);

        var jwt = new JwtSecurityToken(
            issuer: _tokenOptions.Issuer,
            audience: _tokenOptions.Audience,
            expires: expiration,
            notBefore: DateTime.UtcNow,
            claims: BuildClaims(userId, email, userName, roles),
            signingCredentials: signingCredentials
        );

        var token = new JwtSecurityTokenHandler().WriteToken(jwt);
        return new AccessToken(token, expiration);
    }

    public string CreateRefreshToken()
    {
        var number = new byte[32];
        using var random = RandomNumberGenerator.Create();
        random.GetBytes(number);
        return Convert.ToBase64String(number);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _tokenOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = _tokenOptions.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _securityKey
            };

            return handler.ValidateToken(token, parameters, out _);
        }
        catch
        {
            return null;
        }
    }

    public bool IsTokenExpired(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.ValidTo < DateTime.UtcNow;
        }
        catch
        {
            return true;
        }
    }

    private static IEnumerable<Claim> BuildClaims(Guid userId, string email, string userName, List<string> roles)
    {
        var claims = new List<Claim>();
        claims.AddNameIdentifier(userId.ToString());
        claims.AddEmail(email);
        claims.AddName(userName);
        claims.AddRoles(roles.ToArray());
        return claims;
    }
}
