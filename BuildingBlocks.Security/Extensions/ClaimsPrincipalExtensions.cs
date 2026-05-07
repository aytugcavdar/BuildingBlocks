using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace BuildingBlocks.Security.Extensions;

public static class ClaimsPrincipalExtensions
{

    public static List<string>? Claims(this ClaimsPrincipal claimsPrincipal, string claimType)
    {
        var result = claimsPrincipal?.FindAll(claimType)?.Select(x => x.Value).ToList();
        return result;
    }

    public static string? GetClaim(this ClaimsPrincipal claimsPrincipal, string claimType)
    {
        return claimsPrincipal?.Claims(claimType)?.FirstOrDefault();
    }

    public static string? GetUserId(this ClaimsPrincipal claimsPrincipal)
    {
        return claimsPrincipal?.GetClaim(ClaimTypes.NameIdentifier);
    }

    public static string? GetUserEmail(this ClaimsPrincipal claimsPrincipal)
    {
        return claimsPrincipal?.GetClaim(ClaimTypes.Email);
    }

    public static string? GetUserName(this ClaimsPrincipal claimsPrincipal)
    {
        return claimsPrincipal?.GetClaim(ClaimTypes.Name);
    }

    public static List<string>? GetUserRoles(this ClaimsPrincipal claimsPrincipal)
    {
        return claimsPrincipal?.Claims(ClaimTypes.Role);
    }
}
