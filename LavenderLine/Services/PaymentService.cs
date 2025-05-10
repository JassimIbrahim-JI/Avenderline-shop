using LavenderLine.Enums.Order;
using LavenderLine.Enums.Payment;
using LavenderLine.Validate;
using LavenderLine.VerificationServices;
using Microsoft.EntityFrameworkCore;
using NodaTime;


namespace LavenderLine.Services
{
    public interface IPaymentService
    {
        Task<List<Payment>> GetPaymentsByUserIdAsync(string userId);
        Task<List<Payment>> GetAllPaymentsAsync();
        Task<Payment> GetPaymentByIdAsync(long id);
        Task<List<Payment>> GetPaymentsByStatusAsync(PaymentStatus status);
        Task<ValidateResult> CreatePaymentAsync(Payment payment, string paymentMethod);
        Task<ValidateResult> UpdatePaymentStatusAsync(long paymentId, PaymentStatus status);
        Task<bool> UpdatePaymentAsync(Payment payment);
        Task<long> GetPaymentIdFromPaymentIntentId(string paymentIntentId);
        Task<(IEnumerable<Payment> Payments, int TotalCount)> GetPaginatedPaymentsAsync(
       string userId,
       Instant? startDate,
       Instant? endDate,
       PaymentStatus? status,
       int page,
       int pageSize,
       string sortBy,
       string sortDirection);
        Task<int> GetPaymentCountAsync(
         string userId,
         Instant? startDate,
         PaymentStatus? status);

        Task<IEnumerable<Payment>> GetFilteredPaymentsAsync(
       string userId,
       Instant? startDate,
       PaymentStatus? status);

        Task<IEnumerable<Payment>> GetPaymentForExportAsync(string userId, Instant? startDate, PaymentStatus? status);

    }

    public class PaymentService : IPaymentService
    {
        private readonly EcommerceContext _context;
        private readonly INotificationService _notificationService;

        public PaymentService(EcommerceContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<List<Payment>> GetPaymentsByUserIdAsync(string userId)
        {
            return await _context.Payments
                .Where(p => p.UserId == userId)
                .Include(p => p.Order)
                .Include(p => p.User)
                .ToListAsync();
        }

        public async Task<List<Payment>> GetAllPaymentsAsync()
        {
            return await _context.Payments
                .Include(p => p.Order)
                .Include(p => p.User)
                .ToListAsync();
        }

        public async Task<Payment> GetPaymentByIdAsync(long id)
        {
            return await _context.Payments
                .Include(p => p.Order)
                 .ThenInclude(o=>o.User)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<bool> UpdatePaymentAsync(Payment payment)
        {
            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<List<Payment>> GetPaymentsByStatusAsync(PaymentStatus status)
        {
            return await _context.Payments
                .Include(p => p.Order)
                .Include(p => p.User)
                .Where(p => p.Status == status)
                .ToListAsync();
        }

        public async Task<ValidateResult> CreatePaymentAsync(Payment payment,string paymentMethod)
        {
            try 
            {
                payment.PaymentDate = QatarDateTime.Now;
                payment.Status = PaymentStatus.Pending;
                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                // Link payment to order
                var order = await _context.Orders.FindAsync(payment.OrderId);
                if (order != null)
                {
                    order.PaymentId = payment.Id;
                    await _context.SaveChangesAsync();
                }

                return new ValidateResult(true, "Payment created.");
            }
            catch(Exception ex) 
            {
                return new ValidateResult(false, $"Failed: {ex.Message}");
            }
          
        }


        public async Task<ValidateResult> UpdatePaymentStatusAsync(long paymentId, PaymentStatus status)
        {
            try
            {
                var payment = await _context.Payments
                    .Include(p => p.Order)
                    .FirstOrDefaultAsync(p => p.Id == paymentId);

                if (payment == null)
                    return new ValidateResult(false, "Payment not found.");

                payment.Status = status;

                // Sync order status
                if (status == PaymentStatus.Completed && payment.Order != null)
                {
                    payment.Order.Status = OrderStatus.Processing;
                }

                await _context.SaveChangesAsync();
                return new ValidateResult(true, "Status updated.");
            }
            catch (Exception ex)
            {
                return new ValidateResult(false, $"Failed: {ex.Message}");
            }
        }

        public async Task<long> GetPaymentIdFromPaymentIntentId(string paymentIntentId)
        {
            var payment = await _context.Payments
                .Where(p => p.PaymentIntentId == paymentIntentId)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

            return payment;
        }


        public async Task<(IEnumerable<Payment> Payments, int TotalCount)> GetPaginatedPaymentsAsync(
             string userId,
             Instant? startDate,
             Instant? endDate,
             PaymentStatus? status,
             int page,
             int pageSize,
             string sortBy,
             string sortDirection)
        {
            var query = _context.Payments
                .Include(p => p.User)
                .Include(p => p.Order)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(userId))
                query = query.Where(p => p.UserId == userId);
            if (startDate.HasValue)
                query = query.Where(p => p.PaymentDate >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(p => p.PaymentDate <= endDate.Value);
            if (status.HasValue)
                query = query.Where(p => p.Status == status.Value);

            // Apply sorting
            query = sortBy switch
            {
                "Amount" => sortDirection == "asc"
                    ? query.OrderBy(p => p.Amount)
                    : query.OrderByDescending(p => p.Amount),
                "PaymentDate" => sortDirection == "asc"
                    ? query.OrderBy(p => p.PaymentDate)
                    : query.OrderByDescending(p => p.PaymentDate),
                _ => query.OrderByDescending(p => p.PaymentDate)
            };

            var totalCount = await query.CountAsync();
            var payments = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (payments, totalCount);
        }

        public async Task<int> GetPaymentCountAsync(
            string userId,
            Instant? startDate,

            PaymentStatus? status)
        {
            var query = _context.Payments.AsQueryable();

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(p => p.UserId == userId);
            if (startDate.HasValue)
                query = query.Where(p => p.PaymentDate >= startDate.Value);
            if (status.HasValue)
                query = query.Where(p => p.Status == status.Value);

            return await query.CountAsync();
        }

        public async Task<IEnumerable<Payment>> GetFilteredPaymentsAsync(
       string userId,
       Instant? startDate,
       PaymentStatus? status)
        {
            return await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Order)
                .ApplyFilters(userId, startDate, status)
                .ToListAsync();
        }


        public async Task<IEnumerable<Payment>> GetPaymentForExportAsync(string userId, Instant? startDate, PaymentStatus? status)
        {
            return await _context.Payments
                .Include(o => o.User)
                .Include(o => o.Order)
                .ThenInclude(o => o.OrderItems)
                .ThenInclude(oi=>oi.Product)
                .ApplyFilters(userId, startDate, status)
                .ToListAsync();
        }


    }
}
