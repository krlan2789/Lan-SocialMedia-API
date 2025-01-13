using MailKit.Net.Smtp;
using MimeKit;

namespace LanGeng.API.Services;

public class EmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string?> SendAsync(string toEmail, string subject, string message)
    {
        try
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(_configuration["AppName"] ?? "LanGeng", _configuration["EmailSettings:FromEmail"]));
            emailMessage.To.Add(new MailboxAddress("", toEmail));
            emailMessage.Subject = subject;
            emailMessage.Body = new TextPart("plain") { Text = message };

            using var client = new SmtpClient();
            var port = _configuration["EmailSettings:Port"];
            if (string.IsNullOrEmpty(port))
            {
                throw new InvalidOperationException("EmailSettings:Port is not configured.");
            }
            client.Connect(_configuration["EmailSettings:SmtpServer"], int.Parse(port), true);
            client.Authenticate(_configuration["EmailSettings:Username"], _configuration["EmailSettings:Password"]);

            await client.SendAsync(emailMessage);
            client.Disconnect(true);
            return null;
        }
        catch (Exception e)
        {
            return e.Message;
        }
    }
}
