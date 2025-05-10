using LavenderLine.Enums.Payment;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace LavenderLine.VerificationServices
{
    public class NotificationService : INotificationService
    {
        private readonly IEmailSenderService _emailSender;
        private readonly IUserService _userService;
        private readonly EcommerceContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpContextAccessor _httpContext;


        public NotificationService(IUserService userService, IEmailSenderService emailSender, EcommerceContext context, IWebHostEnvironment env, IConfiguration config, IHttpContextAccessor httpContext)
        {
            _userService = userService;
            _emailSender = emailSender;
            _context = context;
            _env = env;
            _httpContext = httpContext;
        }

        public async Task CreateOrderNotification(Order order)
        {
            var notification = new Notification
            {
                Type = "Order",
                Message = $"New order #{order.OrderId} received",
                RelatedId = order.OrderId.ToString(),
                CreatedAt = QatarDateTime.Now
            };

            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();
        }

        public async Task CreateLoginNotification(string userId)
        {
            var notification = new Notification
            {
                Type = "Login",
                Message = "User successfully logged in",
                UserId = userId,
                CreatedAt = QatarDateTime.Now
            };

            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Notification>> GetUnreadNotifications()
        {
            return await _context.Notifications
                .Where(n => !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .ToListAsync();
        }

        public async Task MarkAsRead(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task SendPaymentStatusChangeNotification(Payment payment, PaymentStatus statusChangeType)
        {
            var templatePath = Path.Combine(_env.WebRootPath, "email-templates", "PaymentsConfrimation.html");

            var order = await _context.Orders
             .Include(o => o.User) 
             .FirstOrDefaultAsync(o => o.OrderId == payment.OrderId);

            if (order == null)
            {
                throw new Exception($"Order not found for payment :{payment.Id}");
            }

            string email = order.UserId != null
                           ? order.User?.Email ?? "N/A"
                           : order.GuestEmail ?? "N/A";

            if (string.IsNullOrEmpty(email))
            {
               // _logger.LogError("No email found for payment {PaymentId}", payment.Id);
                return;
            }

            string fullName = order.UserId != null
                ? order.User?.FullName ?? "Authenticated User"
                : order.GuestFullName ?? "Customer";

            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException("Email template not found.", templatePath);
            }

            string htmlTemplate = await File.ReadAllTextAsync(templatePath);

            var (statusMessage, subject) = statusChangeType switch
            {
                PaymentStatus.Completed => (
                    $"Your payment for order #{payment.OrderId} has been successfully confirmed.",
                    "Payment Confirmed"
                ),
                PaymentStatus.Refunded => (
                    $"Your payment for order #{payment.OrderId} has been refunded.",
                    "Payment Refunded"
                ),
                _ => (string.Empty, "Payment Status Update")
            };

            var request = _httpContext.HttpContext?.Request;
            var baseUrl = $"{request?.Scheme}://{request?.Host}";
            var logoUrl = $"{baseUrl}/svgs/avenderline-dark.svg";

            var message = htmlTemplate
                .Replace("{logoUrl}",logoUrl)
                .Replace("{CustomerName}", fullName)
                .Replace("{StatusMessage}", statusMessage)
                .Replace("{PaymentId}", payment.Id.ToString())
                .Replace("{Amount}", payment.Amount.ToString("0.00"))
                .Replace("{Currency}", "QAR")
                .Replace("{PaymentStatus}", statusChangeType.ToString())
                .Replace("{PaymentDate}", payment.PaymentDate.InZone(QatarDateTime.QatarZone).LocalDateTime.ToString("MM/dd/yyyy", new CultureInfo("en-QA")));

            await _emailSender.SendEmailAsync(email, subject, message);
        }
    }
}
