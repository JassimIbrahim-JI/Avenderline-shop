using LavenderLine.Enums.Order;
using LavenderLine.ViewModels.Analytics;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Extensions;

namespace LavenderLine.Services
{
    public class AnalyticsService
    {
        private readonly EcommerceContext _context;
        private readonly IOrderService _orderService;
        private readonly IPaymentService _paymentService;
        private readonly IUserService _userService;

        public AnalyticsService(EcommerceContext context,
                              IOrderService orderService,
                              IPaymentService paymentService,
                              IUserService userService)
        {
            _context = context;
            _orderService = orderService;
            _paymentService = paymentService;
            _userService = userService;
        }


        public async Task<DashboardDataViewModel> GetDashboardDataAsync()
        {
            return new DashboardDataViewModel
            {
                CurrentUsers = await _context.Users.CountAsync(),
                TotalOrders = await _context.Orders.CountAsync(),
                TotalPayments = await _context.Payments.SumAsync(p => p.Amount),
                PendingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending),
                MonthlyRevenue = await GetMonthlyRevenue(),
                TodayRevenue = await GetTodayRevenue(),
                PaymentMethodBreakdown = await GetPaymentMethodBreakdown(),
                RecentOrders = await GetRecentOrders(),
                RecentPayments = await GetRecentPayments(),
                NewCustomers = await GetNewCustomers()
            };
        }

        private async Task<decimal> GetMonthlyRevenue()
        {
            var currentInstant = SystemClock.Instance.GetCurrentInstant();
            var startDate = currentInstant.Minus(Duration.FromDays(30));

            return await _context.Payments
                .Where(p => p.PaymentDate >= startDate)
                .SumAsync(p => p.Amount);
        }

        private async Task<decimal> GetTodayRevenue()
        {
            var qatarClock = SystemClock.Instance.InZone(QatarDateTime.QatarZone);
            var startOfDay = qatarClock.GetCurrentDate().AtStartOfDayInZone(qatarClock.Zone).ToInstant();

            return await _context.Payments
                .Where(p => p.PaymentDate >= startOfDay)
                .SumAsync(p => p.Amount);
        }

        private async Task<Dictionary<string, decimal>> GetPaymentMethodBreakdown()
        {
            return await _context.Payments
                .GroupBy(p => p.PaymentMethod)
                .Select(g => new { Method = g.Key, Total = g.Sum(p => p.Amount) })
                .ToDictionaryAsync(x => x.Method, x => x.Total);
        }

        private async Task<List<Order>> GetRecentOrders(int count = 5)
        {
            return await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(count)
                .ToListAsync();
        }

        private async Task<List<Payment>> GetRecentPayments(int count = 5)
        {
            return await _context.Payments
                .Include(p => p.Order)
                .OrderByDescending(p => p.PaymentDate)
                .Take(count)
                .ToListAsync();
        }

        private async Task<List<ApplicationUser>> GetNewCustomers(int count = 5)
        {
            return await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(count)
                .ToListAsync();
        }
    }
}
