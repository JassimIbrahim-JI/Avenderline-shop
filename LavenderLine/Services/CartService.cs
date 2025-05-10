using LavenderLine.Exceptions;
using Microsoft.EntityFrameworkCore;
using SendGrid.Helpers.Errors.Model;

namespace LavenderLine.Services
{
    public interface ICartService
    {
        Task AddToCartAsync(string userId, AddToCartRequest request);
        Task RemoveFromCartAsync(string userId, RemoveCartItemRequest request);
        Task<CartDto> GetCartAsync(string userId);
        Task ClearCartAsync(string userId);
        Task MergeCartsAsync(string tempUserId, string authenticatedUserId);

    }

    public class CartService : ICartService
    {
        private readonly EcommerceContext _context;
        private readonly ILogger<CartService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public CartService(EcommerceContext context, ILogger<CartService> logger, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }


        public async Task AddToCartAsync(string userId, AddToCartRequest request)
        {
            try 
            {
                var product = await ValidateCartRequest(request);
                var existingItem = await FindExistingCartItem(userId, request);

                var totalQty = request.Quantity + (existingItem?.Quantity ?? 0);
                if (totalQty > product.Quantity)
                    throw new InsufficientStockException(
                        $"Only {product.Quantity} available. You already have {existingItem?.Quantity ?? 0} in cart.");

                product.Quantity -= request.Quantity;
                if (existingItem != null)
                {
                    existingItem.Quantity += request.Quantity;
                    existingItem.SpecialRequest = request.SpecialRequest;
                }
                else
                {
                    var newItem = new CartItem
                    {
                        UserId = userId,
                        ProductId = request.ProductId,
                        Quantity = request.Quantity,
                        Length = request.Length,
                        Size = request.Size,
                        Price = product.Price,
                        SpecialRequest = request.SpecialRequest
                    };
                    await _context.CartItems.AddAsync(newItem);
                }
                await _context.SaveChangesAsync();
            }
            catch 
            {
                throw; // Re-throw for controller error handling
            }

        }


        public async Task RemoveFromCartAsync(string userId, RemoveCartItemRequest request)
        {
            try
            {
                var item = await _context.CartItems
             .FirstOrDefaultAsync(i =>
                 i.UserId == userId &&
                 i.ProductId == request.ProductId &&
                 i.Length == request.Length &&
                 i.Size == request.Size);

                if (item != null)
                {   
                  var product = await _context.Products.FindAsync(item.ProductId);
                  product.Quantity += item.Quantity; 
                  
                   _context.CartItems.Remove(item);
                  await _context.SaveChangesAsync();
                }
            }
            catch 
            {
                throw;
            }
         
        }

        public async Task<CartDto> GetCartAsync(string userId)
        {
            var items = await _context.CartItems
                .Where(i => i.UserId == userId)
                .Include(i => i.Product)
                .Select(i => new CartItemDto(
                    i.ProductId,
                    i.Product.Name,
                    i.Product.ImageUrl,
                    i.Price,
                    i.Quantity,
                    i.Length,
                    i.Size,
                    i.SpecialRequest,
                    i.Product.Quantity
                ))
                .ToListAsync();

            return new CartDto(
                items,
                items.Sum(i => i.Price * i.Quantity),
                items.Count
            );
        }
        public async Task ClearCartAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                // Handle guest cart from session
                _httpContextAccessor.HttpContext.Session.Remove("GuestCartId");
                return;
            }

            var items = await _context.CartItems
                .Where(i => i.UserId == userId)
                .ToListAsync();

            _context.CartItems.RemoveRange(items);
            await _context.SaveChangesAsync();
        }

        public async Task MergeCartsAsync(string tempUserId, string authenticatedUserId)
        {
            if (string.IsNullOrEmpty(tempUserId) || string.IsNullOrEmpty(authenticatedUserId))
            {
                throw new ArgumentException("User IDs cannot be null or empty.");
            }
            try 
            {
                var tempCart = await _context.CartItems
                  .Where(i => i.UserId == tempUserId)
                  .ToListAsync();
                if (!tempCart.Any()) return;

                foreach (var item in tempCart)
                {
                    var existingItem = await _context.CartItems
                        .FirstOrDefaultAsync(i =>
                            i.UserId == authenticatedUserId &&
                            i.ProductId == item.ProductId &&
                            i.Length == item.Length &&
                            i.Size == item.Size);

                    if (existingItem != null)
                    {
                        existingItem.Quantity += item.Quantity;
                    }
                    else
                    {
                        item.UserId = authenticatedUserId;
                        _context.CartItems.Add(item);
                    }
                    _context.CartItems.RemoveRange(tempCart);
                    await _context.SaveChangesAsync();
                }

            }
            catch 
            {
                throw;
            }
          
        }

        //Helpers
        private async Task<Product> ValidateCartRequest(AddToCartRequest request)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductId == request.ProductId && p.IsActive)
                ?? throw new NotFoundException($"Product {request.ProductId} not found");

           
            if (!product.Lengths.Contains(request.Length.Trim()))
            {
                throw new InvalidOperationException("Selected length not available");
            }

          
            if (!product.Sizes.Contains(request.Size.Trim()))
            {
                throw new InvalidOperationException("Selected size not available");
            }

            if (request.SpecialRequest?.Length > 200)
            {
                throw new InvalidOperationException("Special request exceeds 200 characters");
            }

            
            if (request.Quantity > product.Quantity)
            {
                throw new InsufficientStockException($"Only {product.Quantity} available in stock");
            }

            return product;
        }
        private async Task<CartItem> FindExistingCartItem(string userId, AddToCartRequest request)
        {
            var item = await _context.CartItems
                .FirstOrDefaultAsync(i =>
                    i.UserId == userId &&
                    i.ProductId == request.ProductId &&
                    i.Length == request.Length &&
                    i.Size == request.Size
                    );
            return item;
        }

    }
}
