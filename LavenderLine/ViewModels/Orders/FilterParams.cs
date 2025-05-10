using LavenderLine.Enums.Order;
using NodaTime;

namespace LavenderLine.ViewModels.Orders
{
    public class FilterParams
    {
        public string UserName { get; set; }
        public Instant? StartDate { get; set; }
        public OrderStatus? Status { get; set; }
    }
}
