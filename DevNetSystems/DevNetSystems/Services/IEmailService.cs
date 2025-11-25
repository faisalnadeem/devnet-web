namespace DevNetSystems.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string toEmail, string subject, string message, string fromName, string fromEmail);
    }
}

