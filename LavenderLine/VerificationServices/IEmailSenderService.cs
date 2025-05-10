namespace LavenderLine.EmailServices
{
    public interface IEmailSenderService
    {
        Task SendEmailAsync(string email, string subject, string HtmlMessage);
    }
}
