using Microsoft.EntityFrameworkCore;
using SendGrid.Helpers.Errors.Model;

namespace LavenderLine.Services
{
    public interface IWishlistService
    {
        Task<WishlistDto> GetWishlistAsync(string userId);
        Task<bool> ToggleFavoriteAsync(int productId, string userId);
        Task<bool> RemoveFromWishlist(int productId, string userId);
        Task<int> GetWishlistCountAsync(string userId);


    }
    public class WishlistService : IWishlistService
    {
        private readonly EcommerceContext _context;
        private readonly ILogger<WishlistService> _logger;

        public WishlistService(EcommerceContext context, ILogger<WishlistService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<WishlistDto> GetWishlistAsync(string userId)
        {
            var items = await _context.WishlistItems
                .Where(w => w.UserId == userId).AsNoTracking()
                .Include(w => w.Product)
                .Select(w => new WishlistItemDto
                (
                     w.ProductId,
                     w.Product.Name,
                     w.Product.Price,
                     w.Product.OriginalPrice,
                     w.Product.ImageUrl,
                     w.Product.Quantity,
                     w.Product.Category.Name,
                     true
                ))
                .ToListAsync();

            return new WishlistDto
            (
                 items,
                 items.Count
            );
        }

        public async Task<bool> ToggleFavoriteAsync(int productId, string userId)
        {
            var execution = _context.Database.CreateExecutionStrategy();
            return await execution.ExecuteAsync(async () =>
            {
                    var existingItem = await _context.WishlistItems
                        .FirstOrDefaultAsync(w => w.ProductId == productId && w.UserId == userId);

                    if (existingItem != null)
                    {
                        _context.WishlistItems.Remove(existingItem);
                        await _context.SaveChangesAsync();
                        return false;
                    }

                    await ValidateProductForWishlist(productId);

                    var newItem = new WishlistItem
                    {
                        UserId = userId,
                        ProductId = productId,
                        AddedDate = QatarDateTime.Now
                    };

                    await _context.WishlistItems.AddAsync(newItem);
                    await _context.SaveChangesAsync();
                    return true;
            });
          
        }
        public async Task<bool> RemoveFromWishlist(int productId, string userId)
        {
            var execution = _context.Database.CreateExecutionStrategy();
            return await execution.ExecuteAsync(async () =>
            {
                    var item = await _context.WishlistItems
                 .FirstOrDefaultAsync(w => w.ProductId == productId && w.UserId == userId);
                if (item == null) return false;

                _context.WishlistItems.Remove(item);
                await _context.SaveChangesAsync();
                return true;
            });
          
        }


        private async Task ValidateProductForWishlist(int productId)
        {
            var productExists = await _context.Products
                .AnyAsync(p => p.ProductId == productId && p.IsActive);

            if (!productExists)
                throw new NotFoundException($"Product {productId} not available");
        }

        public async Task<int> GetWishlistCountAsync(string userId)
        {
            return await _context.WishlistItems
         .CountAsync(w => w.UserId == userId);
        }
    }


}
