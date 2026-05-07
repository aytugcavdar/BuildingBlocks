using System;
using System.Collections.Generic;
using System.Text;

namespace BuildingBlocks.Messaging.Email;

public interface IEmailService
{
    Task<bool> SendEmailAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default);
    Task<bool> SendTemplateEmailAsync(
        string to,
        string toName,
        string subject,
        string templateName,
        Dictionary<string, object> templateData,
        CancellationToken cancellationToken = default);
    Task<bool> SendEmailConfirmationAsync(
        string email,
        string firstName,
        string confirmationToken,
        CancellationToken cancellationToken = default);
    Task<bool> SendWelcomeEmailAsync(
        string email,
        string firstName,
        CancellationToken cancellationToken = default);
    Task<bool> SendPasswordResetEmailAsync(
        string email,
        string firstName,
        string resetToken,
        CancellationToken cancellationToken = default);
}
