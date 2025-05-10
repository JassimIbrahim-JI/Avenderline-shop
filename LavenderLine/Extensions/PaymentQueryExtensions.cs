using LavenderLine.Enums.Payment;
using NodaTime;

namespace LavenderLine.Extensions
{
    public static class PaymentQueryExtensions
    {
        public static IQueryable<Payment> ApplyFilters(
            this IQueryable<Payment> query,
            string userId,
            Instant? startDate,
 
            PaymentStatus? status)
        {
            if (!string.IsNullOrEmpty(userId))
                query = query.Where(p => p.UserId == userId);

            if (startDate.HasValue)
                query = query.Where(p => p.PaymentDate >= startDate.Value);

         

            if (status.HasValue)
                query = query.Where(p => p.Status == status.Value);

            return query;
        }

        public static IQueryable<Payment> ApplySorting(
            this IQueryable<Payment> query,
            string sortBy,
            string sortDirection)
        {
            return sortBy switch
            {
                "Amount" => sortDirection == "asc"
                    ? query.OrderBy(p => p.Amount)
                    : query.OrderByDescending(p => p.Amount),
                "PaymentDate" => sortDirection == "asc"
                    ? query.OrderBy(p => p.PaymentDate)
                    : query.OrderByDescending(p => p.PaymentDate),
                _ => query.OrderByDescending(p => p.PaymentDate)
            };
        }
    }
}
