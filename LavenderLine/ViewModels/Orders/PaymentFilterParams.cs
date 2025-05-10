using LavenderLine.Enums.Payment;
using NodaTime;

namespace LavenderLine.ViewModels.Orders
{
    public class PaymentFilterParams
    {
        public string UserId { get; set; }
        public Instant? StartDate { get; set; }
        public Instant? EndDate { get; set; }
        public PaymentStatus? Status { get; set; }
    }
}
