using BuildingBlocks.Core.Security.Constants;
using Microsoft.AspNetCore.Authorization;

namespace BuildingBlocks.CrossCutting.Authentication;

/// <summary>
/// Merkezi authorization policy tanımları.
/// AuthenticationExtensions'a register edilir.
/// Politika adları için sabit string'ler kullanılır — magic string yok.
/// </summary>
public static class AuthorizationPolicies
{
    // ─── Politika Adı Sabitleri ──────────────────────────────────────────────────────
    public const string AdminOnly = "AdminOnly";
    public const string UserOrAdmin = "UserOrAdmin";
    public const string CanManageProducts = "CanManageProducts";
    public const string CanManageOrders = "CanManageOrders";
    public const string CanManageUsers = "CanManageUsers";

    /// <summary>
    /// Tüm standart politikaları register eder.
    /// AuthenticationExtensions.AddJwtAuthentication() içinden çağrılır.
    /// </summary>
    public static void Register(AuthorizationOptions options)
    {
        options.AddPolicy(AdminOnly, policy =>
            policy.RequireRole(GeneralOperationClaims.Admin));

        options.AddPolicy(UserOrAdmin, policy =>
            policy.RequireRole(GeneralOperationClaims.User, GeneralOperationClaims.Admin));

        options.AddPolicy(CanManageProducts, policy =>
            policy.RequireRole(GeneralOperationClaims.Admin));

        options.AddPolicy(CanManageOrders, policy =>
            policy.RequireRole(GeneralOperationClaims.Admin, GeneralOperationClaims.Moderator));

        options.AddPolicy(CanManageUsers, policy =>
            policy.RequireRole(GeneralOperationClaims.Admin));
    }
}
