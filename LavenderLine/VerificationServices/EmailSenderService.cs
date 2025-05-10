using LavenderLine.Settings;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace LavenderLine.EmailServices
{
    public class EmailSenderService : IEmailSenderService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailSenderService> _logger;
        public EmailSenderService(IOptions<EmailSettings> emailSettings, ILogger<EmailSenderService> logger, IWebHostEnvironment env)
        {

            _emailSettings = emailSettings.Value;
            _logger = logger;

            ValidateConfiguration();
        }

        private void ValidateConfiguration()
        {
            if (string.IsNullOrEmpty(_emailSettings.ApiKey))
                throw new ArgumentNullException(nameof(EmailSettings.ApiKey));
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                var client = new SendGridClient(_emailSettings.ApiKey);
                var from = new EmailAddress(_emailSettings.EmailAddress, _emailSettings.FromName);
                var to = new EmailAddress(email);

                var msg = MailHelper.CreateSingleEmail(
                    from,
                    to,
                    subject,
                    plainTextContent: "Please view this email in a modern email client",
                    htmlMessage
                );

                var response = await client.SendEmailAsync(msg);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Body.ReadAsStringAsync();
                    _logger.LogError("Email failed: {StatusCode} - {Error}",
                        response.StatusCode, errorBody);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Email sending failed");
                throw;
            }
        }

    }
}
