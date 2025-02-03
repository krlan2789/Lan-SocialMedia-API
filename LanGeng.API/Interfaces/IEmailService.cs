namespace LanGeng.API.Interfaces;

public interface IEmailService
{
    public Task<string?> SendAsync(string toEmail, string subject, string message);
}
