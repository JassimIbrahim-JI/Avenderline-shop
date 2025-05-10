using LavenderLine.Validate;
using Microsoft.EntityFrameworkCore;

namespace LavenderLine.Services
{
    public interface IOrderItemService
    {
        Task<(IEnumerable<OrderItem> Items, int TotalCount)> GetPagedOrderItemsAsync(int orderId, int pageNumber, int pageSize);
        Task<IEnumerable<OrderItem>> GetAllOrderItemsAsync();
        Task<ValidateResult> UpdateOrderItemAsync(OrderItem orderItem);
        Task<ValidateResult> AddOrderItemAsync(OrderItem orderItem);
        Task<List<OrderItem>> GetOrderItemsByOrderIdAsync(long orderId);
        Task<ValidateResult> DeleteOrderItemAsync(int orderItemId);

    }
    public class OrderItemService : IOrderItemService
    {

        private readonly EcommerceContext _context;
        public OrderItemService(EcommerceContext context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<OrderItem> Items, int TotalCount)> GetPagedOrderItemsAsync(int orderId, int pageNumber, int pageSize)
        {
            var query = GetQueryable()
                .Where(item => item.OrderId == orderId);

            var totalCount = await query.CountAsync();

            var items = await query.Skip((pageNumber - 1) * pageSize)
                                    .Take(pageSize)
                                    .ToListAsync();

            return (items, totalCount);
        }

        public async Task<IEnumerable<OrderItem>> GetAllOrderItemsAsync()
        {
            return await _context.OrderItems.ToListAsync();
        }

        public async Task<ValidateResult> UpdateOrderItemAsync(OrderItem orderItem)
        {
            // Check if the order item exists
            var existingItem = await _context.OrderItems.FirstOrDefaultAsync(oi => oi.OrderItemId == orderItem.OrderItemId);
            if (existingItem == null)
            {
                return new ValidateResult(false, "Order item not found.");
            }

            // Validate the order item properties (you can add more validation logic as needed)
            if (orderItem.Quantity <= 0)
            {
                return new ValidateResult(false, "Quantity must be greater than zero.");
            }

            // Update properties
            existingItem.ProductId = orderItem.ProductId;
            existingItem.Quantity = orderItem.Quantity;
            existingItem.Price = orderItem.Price;

            // Save changes
            await _context.SaveChangesAsync();

            return new ValidateResult(true, "Order item updated successfully.");
        }

        public async Task<ValidateResult> AddOrderItemAsync(OrderItem orderItem)
        {
            if (orderItem.Quantity <= 0)
            {
                return new ValidateResult(false, "Quantity must be greater than zero.");
            }

            await _context.OrderItems.AddAsync(orderItem);
            return new ValidateResult(true, "Order item added successfully.");
        }

        public async Task<List<OrderItem>> GetOrderItemsByOrderIdAsync(long orderId)
        {
            return await _context.OrderItems
                .Include(item => item.Product) 
                .Where(item => item.OrderId == orderId)
                .ToListAsync();
        }

        public async Task<ValidateResult> DeleteOrderItemAsync(int orderItemId)
        {
            var orderItem = await _context.OrderItems.FindAsync(orderItemId);
            if (orderItem == null)
            {
                return new ValidateResult(false, "Order item not found.");
            }

            _context.OrderItems.Remove(orderItem);
            await _context.SaveChangesAsync();
            return new ValidateResult(true, "Order item deleted successfully.");
        }



        private IQueryable<OrderItem> GetQueryable()
        {
            return _context.OrderItems.AsQueryable();
        }

    }
}
