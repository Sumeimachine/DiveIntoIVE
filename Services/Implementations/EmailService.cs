using DiveIntoIVE.Configurations;
using DiveIntoIVE.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;

namespace DiveIntoIVE.Services.Implementations;

//public class EmailService : IEmailService
//{
//    private readonly EmailSettings _emailSettings;
//    private readonly AppSettings _appSettings;
//    private readonly ILogger<EmailService> _logger;

//    public EmailService(
//        IOptions<EmailSettings> emailOptions,
//        IOptions<AppSettings> appOptions,
//        ILogger<EmailService> logger)
//    {
//        _emailSettings = emailOptions.Value;
//        _appSettings = appOptions.Value;
//        _logger = logger;
//    }

//    public async Task SendVerificationEmailAsync(string email, string token)
//    {
//        var verifyLink = $"{_appSettings.BaseUrl}/api/auth/verify-email?token={token}";

//        var message = new MimeMessage();
//        message.From.Add(new MailboxAddress("DiveIntoIVE", _emailSettings.Email));
//        message.To.Add(MailboxAddress.Parse(email));
//        message.Subject = "Verify your DiveIntoIVE account";

//        message.Body = new TextPart("html")
//        {
//            Text = $"Click <a href='{verifyLink}'>Verify Email</a>"
//        };

//        using var smtp = new SmtpClient();

//        await smtp.ConnectAsync(_emailSettings.Host, _emailSettings.Port, SecureSocketOptions.StartTls);
//        await smtp.AuthenticateAsync(_emailSettings.Email, _emailSettings.Password);
//        await smtp.SendAsync(message);
//        await smtp.DisconnectAsync(true);
//    }
//}
public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly AppSettings _appSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IOptions<EmailSettings> emailOptions,
        IOptions<AppSettings> appOptions,
        ILogger<EmailService> logger)
    {
        _emailSettings = emailOptions.Value;
        _appSettings = appOptions.Value;
        _logger = logger;
    }

    public async Task SendVerificationEmailAsync(string email, string token)
    {
        var verifyLink = $"{_appSettings.BaseUrl}/api/auth/verify-email?token={token}";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("DiveIntoIVE", _emailSettings.Email));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = "Verify your DiveIntoIVE account";

        message.Body = new TextPart("html")
        {
            Text = $"Click <a href='{verifyLink}'>Verify Email</a>"
        };

        await SendEmail(message);
    }

    public async Task SendPasswordResetEmailAsync(string email, string token)
    {
        var resetLink = $"{_appSettings.BaseUrl}/reset-password?token={token}";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("DiveIntoIVE", _emailSettings.Email));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = "Reset your password";

        message.Body = new TextPart("html")
        {
            Text = $"""
            <h2>Password Reset</h2>
            <p>Click the link below to reset your password:</p>
            <a href="{resetLink}">Reset Password</a>
            """
        };

        await SendEmail(message);
    }

    // helper method (optional but cleaner)
    private async Task SendEmail(MimeMessage message)
    {
        try
        {
            using var smtp = new SmtpClient();

            await smtp.ConnectAsync(_emailSettings.Host, _emailSettings.Port, SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync(_emailSettings.Email, _emailSettings.Password);

            await smtp.SendAsync(message);

            await smtp.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email sending failed");
            throw;
        }
    }
}