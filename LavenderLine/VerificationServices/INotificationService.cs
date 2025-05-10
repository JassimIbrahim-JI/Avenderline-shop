using LavenderLine.Enums.Payment;

namespace LavenderLine.VerificationServices
{
    public interface INotificationService
    {
        Task SendPaymentStatusChangeNotification(Payment payment, PaymentStatus statusChangeType);
        Task CreateOrderNotification(Order order);
        Task CreateLoginNotification(string userId);
        Task<List<Notification>> GetUnreadNotifications();
        Task MarkAsRead(int notificationId);
    }
}
