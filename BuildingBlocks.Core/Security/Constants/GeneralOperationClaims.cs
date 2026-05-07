namespace BuildingBlocks.Core.Security.Constants;

/// <summary>
/// Genel sistem claim sabitleri.
/// Servis-spesifik claimler (Product.Add, Order.Read vb.) her servisin kendi katmanında tanımlanmalıdır.
/// Bu sınıf sadece evrensel roller ve temel claim adlarını içerir.
/// </summary>
public static class GeneralOperationClaims
{
    // ─── Temel Roller ──────────────────────────────────────────────────────────
    public const string Admin = "Admin";
    public const string User = "User";
    public const string Moderator = "Moderator";

    // ─── Standart Claim Tip Adları ─────────────────────────────────────────────
    public const string UserId = "sub";
    public const string Email = "email";
    public const string Role = "role";
    public const string Permission = "permission";
}
