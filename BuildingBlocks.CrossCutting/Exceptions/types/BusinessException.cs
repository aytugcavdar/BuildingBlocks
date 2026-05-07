namespace BuildingBlocks.CrossCutting.Exceptions.Types;

/// <summary>
/// İş kuralı ihlallerini temsil eden exception.
/// Kullanıcının yanlış bir şey yaptığı durumlarda fırlatılır.
/// Örnek: "Email already exists", "Insufficient stock"
/// HTTP 400 Bad Request döner.
/// </summary>
public class BusinessException : Exception
{
    public BusinessException(string message) : base(message) { }
}