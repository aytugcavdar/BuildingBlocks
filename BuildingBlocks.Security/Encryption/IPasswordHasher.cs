namespace BuildingBlocks.Security.Encryption;

/// <summary>
/// Şifre hashleme ve doğrulama soyutlaması.
/// Farklı hashing algoritmalarına (HMACSHA512, BCrypt, Argon2) geçiş yapmayı kolaylaştırır.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Şifreyi hashler ve salt ile hash'i döner.
    /// </summary>
    void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt);

    /// <summary>
    /// Verilen şifrenin hash ile eşleşip eşleşmediğini doğrular.
    /// </summary>
    bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt);
}

/// <summary>
/// HMACSHA512 tabanlı IPasswordHasher implementasyonu.
/// </summary>
public class HmacSha512PasswordHasher : IPasswordHasher
{
    public void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        => HashingHelper.CreatePasswordHash(password, out passwordHash, out passwordSalt);

    public bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        => HashingHelper.VerifyPasswordHash(password, passwordHash, passwordSalt);
}
