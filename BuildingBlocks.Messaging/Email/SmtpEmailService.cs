using BuildingBlocks.Messaging.Templates;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Text;

namespace BuildingBlocks.Messaging.Email;

public class SmtpEmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ITemplateService _templateService;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IOptions<EmailOptions> options,
        ITemplateService templateService,
        ILogger<SmtpEmailService> logger)
    {
        _options = options.Value;
        _templateService = templateService;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            
            var mimeMessage = new MimeMessage();

            
            mimeMessage.From.Add(new MailboxAddress(
                _options.SenderName,
                _options.SenderEmail
            ));

            
            mimeMessage.To.Add(new MailboxAddress(
                message.ToName ?? message.To,
                message.To
            ));

            
            foreach (var cc in message.Cc)
            {
                mimeMessage.Cc.Add(MailboxAddress.Parse(cc));
            }

           
            foreach (var bcc in message.Bcc)
            {
                mimeMessage.Bcc.Add(MailboxAddress.Parse(bcc));
            }

            
            mimeMessage.Subject = message.Subject;

            
            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = message.Body
            };

            
            if (!string.IsNullOrEmpty(message.PlainTextBody))
            {
                bodyBuilder.TextBody = message.PlainTextBody;
            }

            mimeMessage.Body = bodyBuilder.ToMessageBody();

            
            using var client = new SmtpClient();

            await client.ConnectAsync(
                _options.SmtpServer,
                _options.SmtpPort,
                _options.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
                cancellationToken
            );

            await client.AuthenticateAsync(
                _options.Username,
                _options.Password,
                cancellationToken
            );

            await client.SendAsync(mimeMessage, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation(
                "📧 Email sent successfully to {To}: {Subject}",
                message.To,
                message.Subject
            );

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ Failed to send email to {To}: {Subject}",
                message.To,
                message.Subject
            );

            return false;
        }
    }

    public async Task<bool> SendTemplateEmailAsync(
        string to,
        string toName,
        string subject,
        string templateName,
        Dictionary<string, object> templateData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Template'i render et
            var htmlBody = await _templateService.RenderTemplateAsync(
                templateName,
                templateData
            );

            var message = new EmailMessage
            {
                To = to,
                ToName = toName,
                Subject = subject,
                Body = htmlBody,
                TemplateName = templateName,
                TemplateData = templateData
            };

            return await SendEmailAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ Failed to send template email: {Template} to {To}",
                templateName,
                to
            );

            return false;
        }
    }

    public async Task<bool> SendEmailConfirmationAsync(
        string email,
        string firstName,
        string confirmationToken,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = string.IsNullOrEmpty(_options.AppBaseUrl) ? "https://localhost" : _options.AppBaseUrl.TrimEnd('/');
        var confirmationUrl = $"{baseUrl}/auth/confirm-email?token={confirmationToken}&email={email}";

        var templateData = new Dictionary<string, object>
        {
            { "FirstName", firstName },
            { "ConfirmationUrl", confirmationUrl },
            { "Year", DateTime.Now.Year }
        };

        return await SendTemplateEmailAsync(
            to: email,
            toName: firstName,
            subject: "Confirm Your Email Address",
            templateName: "EmailConfirmation",
            templateData: templateData,
            cancellationToken: cancellationToken
        );
    }

    public async Task<bool> SendWelcomeEmailAsync(
        string email,
        string firstName,
        CancellationToken cancellationToken = default)
    {
        var templateData = new Dictionary<string, object>
        {
            { "FirstName", firstName },
            { "Year", DateTime.Now.Year }
        };

        return await SendTemplateEmailAsync(
            to: email,
            toName: firstName,
            subject: "Welcome to MicroECommerce!",
            templateName: "Welcome",
            templateData: templateData,
            cancellationToken: cancellationToken
        );
    }

    public async Task<bool> SendPasswordResetEmailAsync(
        string email,
        string firstName,
        string resetToken,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = string.IsNullOrEmpty(_options.AppBaseUrl) ? "https://localhost" : _options.AppBaseUrl.TrimEnd('/');
        var resetUrl = $"{baseUrl}/auth/reset-password?token={resetToken}&email={email}";

        var templateData = new Dictionary<string, object>
        {
            { "FirstName", firstName },
            { "ResetUrl", resetUrl },
            { "Year", DateTime.Now.Year }
        };

        return await SendTemplateEmailAsync(
            to: email,
            toName: firstName,
            subject: "Reset Your Password",
            templateName: "PasswordReset",
            templateData: templateData,
            cancellationToken: cancellationToken
        );
    }
}
