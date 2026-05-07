using System;
using System.Collections.Generic;
using System.Text;

namespace BuildingBlocks.Security.EmailAuth;

public class EmailAuthService : IEmailAuthService
{
    public string GenerateEmailConfirmationToken()
    {
        return Guid.NewGuid().ToString("N");
    }

    public string GeneratePasswordResetToken()
    {
        return $"{Guid.NewGuid():N}{Guid.NewGuid():N}";
    }

    public bool ValidateEmailConfirmationToken(string token, string storedToken)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(storedToken))
            return false;

        return string.Equals(
            token.Trim(),
            storedToken.Trim(),
            StringComparison.OrdinalIgnoreCase
        );
    
    }

    public bool ValidatePasswordResetToken(string token, string storedToken, DateTime tokenCreatedDate, int expirationHours = 24)
    {
        if (!ValidateEmailConfirmationToken(token, storedToken))
            return false;

        
        var expirationDate = tokenCreatedDate.AddHours(expirationHours);
        if (DateTime.UtcNow > expirationDate)
            return false;

        return true;
    }
}
