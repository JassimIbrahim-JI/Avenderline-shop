namespace LavenderLine.ViewModels.Analytics
{
    public class DashboardDataViewModel
    {
        public int CurrentUsers { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalPayments { get; set; }

        public int PendingOrders { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public decimal TodayRevenue { get; set; }
        public Dictionary<string, decimal> PaymentMethodBreakdown { get; set; }
        public List<Order> RecentOrders { get; set; }
        public List<Payment> RecentPayments { get; set; }
        public List<ApplicationUser> NewCustomers { get; set; }

    }
}
