using LavenderLine.Enums.Order;
using NodaTime;

namespace LavenderLine.Extensions
{
    public static class QueryExtensions
    {
        public static IQueryable<Order> ApplyFilters(
            this IQueryable<Order> query,
            string userName,
            Instant? startDate,
            OrderStatus? status)
        {
            if (!string.IsNullOrEmpty(userName))
            {
                query = query.Where(o =>
                    (o.User != null && o.User.FullName.Contains(userName)) ||
                    o.GuestFullName.Contains(userName));
            }

            if (startDate.HasValue)
            {
                query = query.Where(o => o.OrderDate >= startDate.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(o => o.Status == status.Value);
            }

            return query;
        }

        public static IQueryable<Order> ApplySorting(
            this IQueryable<Order> query,
            string sortBy,
            string sortDirection)
        {
            return sortBy switch
            {
                "TotalAmount" => sortDirection == "asc"
                    ? query.OrderBy(o => o.TotalAmount)
                    : query.OrderByDescending(o => o.TotalAmount),
                "OrderDate" => sortDirection == "asc"
                    ? query.OrderBy(o => o.OrderDate)
                    : query.OrderByDescending(o => o.OrderDate),
                _ => query.OrderByDescending(o => o.OrderDate)
            };
        }
    }
}
