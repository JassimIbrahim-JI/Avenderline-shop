using LavenderLine.Enums.Order;
using LavenderLine.Validate;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using SendGrid.Helpers.Errors.Model;

namespace LavenderLine.Services
{
    public interface IOrderService
    {
        Task<Order> CreateOrderAsync(Order order);
        Task<Order> GetOrderByIdAsync(long orderId);
        Task<IEnumerable<Order>> GetOrdersByUserIdAndStatusAsync(string userId, OrderStatus status);
        Task<IEnumerable<Order>> GetAllOrdersByUserIdAsync(string userId);
        Task<IEnumerable<Order>> GetAllOrdersAsync();
        Task<ValidateResult> UpdateOrderAsync(Order order, List<long> deletedItems);
        Task<bool> UpdateOrderStatusAsync(long orderId, OrderStatus newStatus);
        Task<ValidateResult> DeleteOrderAsync(long orderId);
        Task<(IEnumerable<Order> Orders, int TotalCount)> GetPaginatedOrdersAsync(
            string userName,
            Instant? startDate,
            OrderStatus? status,
            int page,
            int pageSize,
            string sortBy,
            string sortDirection);

        Task<int> GetOrderCountAsync(string userName, Instant? startDate, OrderStatus? status);
        Task<IEnumerable<Order>> GetOrdersForExportAsync(string userName, Instant? startDate, OrderStatus? status);

        Task<(List<Order>, int)> GetOrdersAsync(
    string userId = null,
    OrderStatus? status = null,
    string search = null,
    int page = 1,
    int pageSize = 10);
    }
    public class OrderService : IOrderService
    {
        private readonly EcommerceContext _context;
        private readonly IOrderItemService _orderItemRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public OrderService(IOrderItemService orderItemRepository, EcommerceContext context, IHttpContextAccessor httpContextAccessor)
        {
            _orderItemRepository = orderItemRepository;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Order> CreateOrderAsync(Order order)
        {
            // Calculate total and set necessary properties
            order.CalculateTotalAmount();
            order.OrderDate = QatarDateTime.Now;

            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            return order;
        }

        public async Task<Order> GetOrderByIdAsync(long orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .ThenInclude(p => p.Category)
                .Include(o => o.Payment)
                .Include(o => o.User)
                .Include(o=>o.OrderHistories)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            return order ?? throw new NotFoundException($"Order with ID {orderId} not found");
        }


        public async Task<IEnumerable<Order>> GetOrdersByUserIdAndStatusAsync(string userId, OrderStatus status)
        {
            return await _context.Orders.Include(o => o.OrderItems)
                 .Where(o => o.UserId == userId && o.Status == status).ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetAllOrdersByUserIdAsync(string userId)
        {
            return await _context.Orders
                .Where(o => o.UserId == userId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetAllOrdersAsync()
        {
            return await _context.Orders.ToListAsync();
        }

        public async Task<ValidateResult> UpdateOrderAsync(Order order, List<long> deletedItems)
        {
            var executionStrategy = _context.Database.CreateExecutionStrategy();
            return await executionStrategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var existingOrder = await _context.Orders
                        .Include(o => o.OrderItems)
                        .Include(o => o.OrderHistories)
                        .FirstOrDefaultAsync(o => o.OrderId == order.OrderId);

                    if (existingOrder == null)
                        return new ValidateResult(false, "Order not found.");


                    var changes = new List<OrderHistory>();
                    var userId = _httpContextAccessor.HttpContext?.User.GetUserId(_httpContextAccessor.HttpContext) ?? "System";

                    // Track status changes
                    if (existingOrder.Status != order.Status)
                    {
                        changes.Add(new OrderHistory
                        {
                            OrderId = order.OrderId,
                            ChangedBy = userId,
                            ChangeDate = QatarDateTime.Now,
                            FieldChanged = nameof(Order.Status),
                            OldValue = existingOrder.Status.ToString(),
                            NewValue = order.Status.ToString()
                        });
                    }

                    // Track order item changes
                    foreach (var item in order.OrderItems)
                    {
                        var existingItem = existingOrder.OrderItems
                            .FirstOrDefault(i => i.OrderItemId == item.OrderItemId);

                        if (existingItem != null)
                        {
                            // Track quantity changes
                            if (existingItem.Quantity != item.Quantity)
                            {
                                changes.Add(new OrderHistory
                                {
                                    OrderId = order.OrderId,
                                    ChangedBy = userId,
                                    ChangeDate = QatarDateTime.Now,
                                    FieldChanged = $"{nameof(OrderItem.Quantity)} (Product {item.ProductId})",
                                    OldValue = existingItem.Quantity.ToString(),
                                    NewValue = item.Quantity.ToString()
                                });
                            }

                            // Update other properties...
                        }
                        else
                        {
                            changes.Add(new OrderHistory
                            {
                                OrderId = order.OrderId,
                                ChangedBy = userId,
                                ChangeDate = QatarDateTime.Now,
                                FieldChanged = "Order Items",
                                OldValue = "None",
                                NewValue = $"Added product {item.ProductId}"
                            });
                        }
                    }

                    // Track deleted items
                    foreach (var deletedItemId in deletedItems)
                    {
                        var item = existingOrder.OrderItems.FirstOrDefault(i => i.OrderItemId == deletedItemId);
                        if (item != null)
                        {
                            changes.Add(new OrderHistory
                            {
                                OrderId = order.OrderId,
                                ChangedBy = userId,
                                ChangeDate = QatarDateTime.Now,
                                FieldChanged = "Order Items",
                                OldValue = $"Product {item.ProductId}",
                                NewValue = "Removed"
                            });
                        }
                    }

                    // Apply updates
                    existingOrder.Status = order.Status;
                    existingOrder.ModifiedDate = QatarDateTime.Now;
                    if (changes.Any())
                    {
                        await _context.OrderHistories.AddRangeAsync(changes);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return new ValidateResult(true, "Order updated successfully.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return new ValidateResult(false, $"Update failed: {ex.Message}");
                }
            });
        }

        public async Task<bool> UpdateOrderStatusAsync(long orderId, OrderStatus newStatus)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return false;

            order.Status = newStatus;
            order.ModifiedDate = QatarDateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ValidateResult> DeleteOrderAsync(long orderId)
        {
            var executionStrategy = _context.Database.CreateExecutionStrategy();
            return await executionStrategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var order = await _context.Orders
                    .Include(o => o.Payment)
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);
                    if (order == null)
                        return new ValidateResult(false, "Order not found.");

                    if (order.Status == OrderStatus.Shipped)
                        return new ValidateResult(false, "Cannot delete shipped orders");

                    if (order.Payment != null)
                        _context.Payments.Remove(order.Payment);

                    _context.OrderItems.RemoveRange(order.OrderItems);
                    _context.Orders.Remove(order);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return new ValidateResult(true, "Order deleted successfully.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return new ValidateResult(false, $"Delete failed: {ex.Message}");
                }
            });
        }

        public async Task<(IEnumerable<Order> Orders, int TotalCount)> GetPaginatedOrdersAsync(
      string userName,
      Instant? startDate,
      OrderStatus? status,
      int page,
      int pageSize,
      string sortBy,
      string sortDirection)
        {
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .AsQueryable();

            // Filtering
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

            // Sorting
            query = sortBy switch
            {
                "TotalAmount" => sortDirection == "asc"
                    ? query.OrderBy(o => o.TotalAmount)
                    : query.OrderByDescending(o => o.TotalAmount),
                "OrderDate" => sortDirection == "asc"
                    ? query.OrderBy(o => o.OrderDate)
                    : query.OrderByDescending(o => o.OrderDate),
                _ => query.OrderByDescending(o => o.OrderDate)
            };

            // Pagination
            var totalCount = await query.CountAsync();
            var orders = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (orders, totalCount);
        }

        public async Task<int> GetOrderCountAsync(string userName, Instant? startDate, OrderStatus? status)
        {
            return await _context.Orders
                .Where(o =>
                    (string.IsNullOrEmpty(userName) ||
                        ((o.User != null && o.User.FullName.Contains(userName)) ||
                         (o.GuestFullName != null && o.GuestFullName.Contains(userName)))) &&
                    (!startDate.HasValue || o.OrderDate >= startDate.Value) &&
                    (!status.HasValue || o.Status == status.Value))
                .CountAsync();
        }

        public async Task<IEnumerable<Order>> GetOrdersForExportAsync(string userName, Instant? startDate, OrderStatus? status)
        {
            return await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .ApplyFilters(userName, startDate, status)
                .ToListAsync();
        }

        public async Task<(List<Order>, int)> GetOrdersAsync(string userId = null, OrderStatus? status = null, string search = null, int page = 1, int pageSize = 10)
        {
            var query = _context.Orders
        .Include(o => o.User)
        .Include(o => o.Payment)
        .Include(o => o.OrderItems)
        .ThenInclude(oi => oi.Product)
        .AsQueryable();

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(o => o.UserId == userId);

            if (status.HasValue)
                query = query.Where(o => o.Status == status.Value);

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(o =>
                    o.OrderId.ToString().Contains(search) ||
                    o.User.FullName.Contains(search) ||
                    o.GuestFullName.Contains(search) ||
                    o.User.Email.Contains(search) ||
                    o.GuestEmail.Contains(search));
            }

            var totalCount = await query.CountAsync();

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (orders, totalCount);
        }
    }
}
