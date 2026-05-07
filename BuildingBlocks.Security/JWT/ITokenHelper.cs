using System.Security.Claims;

namespace BuildingBlocks.Security.JWT;

/// <summary>
/// JWT token oluşturma ve doğrulama sözleşmesi.
/// </summary>
public interface ITokenHelper
{
    /// <summary>Verilen kullanıcı bilgileri ile yeni bir access token oluşturur.</summary>
    AccessToken CreateToken(Guid userId, string email, string userName, List<string> roles);

    /// <summary>Güvenli rastgele refresh token oluşturur.</summary>
    string CreateRefreshToken();

    /// <summary>
    /// Token'ı doğrular ve içindeki claims'leri döner.
    /// Token geçersizse null döner.
    /// </summary>
    ClaimsPrincipal? ValidateToken(string token);

    /// <summary>Token'ın süresi dolmuş mu?</summary>
    bool IsTokenExpired(string token);
}
