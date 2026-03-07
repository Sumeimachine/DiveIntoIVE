using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

public class EmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendVerificationEmail(string email, string token)
    {
        var settings = _config.GetSection("EmailSettings");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("DiveIntoIVE_PH", settings["Email"]));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = "Verify your DiveIntoIVE account";

        var verifyLink = $"https://localhost:5173/verify-email?token={token}";

        message.Body = new TextPart("html")
        {
            Text = $"Click <a href='{verifyLink}'>here</a> to verify your email."
        };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(settings["Host"], int.Parse(settings["Port"]), SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(settings["Email"], settings["Password"]);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);
    }
}