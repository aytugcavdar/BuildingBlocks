using System;
using System.Collections.Generic;
using System.Text;

namespace BuildingBlocks.Security.EmailAuth;

public interface IEmailAuthService
{
    string GenerateEmailConfirmationToken();
    bool ValidateEmailConfirmationToken(string token, string storedToken);
    string GeneratePasswordResetToken();
    bool ValidatePasswordResetToken(
        string token,
        string storedToken,
        DateTime tokenCreatedDate,
        int expirationHours = 24);
}
