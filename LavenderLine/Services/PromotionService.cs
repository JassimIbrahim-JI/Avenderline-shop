using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace LavenderLine.Services
{
    public interface IPromotionService
    {
        Task<Promotion> CreatePromotionAsync(Promotion promotion);
        Task<Promotion> GetCurrentPromotionAsync();
        Task<Promotion> GetCurrentPromotionByProductIdAsync(int productId);
        Task<Promotion> UpdatePromotionAsync(Promotion promotion);
        Task<Promotion> GetPromotionByIdAsync(int id);
        Task DeletePromotionAsync(int id);
        Task DeletePromotionByProductIdAsync(int productId);

    }

    public class PromotionService : IPromotionService
    {
        private readonly EcommerceContext _context;

        public PromotionService(EcommerceContext context)
        {
            _context = context;
        }

        public async Task<Promotion> GetCurrentPromotionAsync()
        {
            var currentInstant = SystemClock.Instance.GetCurrentInstant();

            var promotion = await _context.Promotions
                .Where(p => p.StartDate <= currentInstant && p.EndDate >= currentInstant)
                .OrderByDescending(p => p.StartDate)
                .Include(p => p.Product)
                    .ThenInclude(prod => prod.Category)
                .FirstOrDefaultAsync();

            return promotion;
        }

        public async Task<Promotion> GetPromotionByIdAsync(int id)
        {
            return await _context.Promotions
                .Include(p => p.Product) // Include the related Product
                    .ThenInclude(prod => prod.Category) // Include the Category related to the Product
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Promotion> GetCurrentPromotionByProductIdAsync(int productId)
        {
            var currentInstant = SystemClock.Instance.GetCurrentInstant();
            return await _context.Promotions
                .Where(p => p.ProductId == productId && p.EndDate > currentInstant)
                .OrderByDescending(p => p.EndDate)
                .FirstOrDefaultAsync();
        }


        public async Task<Promotion> UpdatePromotionAsync(Promotion promotion)
        {
            // First, ensure the promotion exists
            var existingPromotion = await _context.Promotions.FindAsync(promotion.Id);
            if (existingPromotion == null)
            {
                throw new ArgumentException("Promotion not found.");
            }

            // Update the existing promotion with new values
            existingPromotion.Title = promotion.Title;
            existingPromotion.PromotionText = promotion.PromotionText;
            existingPromotion.DiscountPercentage = promotion.DiscountPercentage;
            existingPromotion.StartDate = promotion.StartDate;
            existingPromotion.EndDate = promotion.EndDate;
            existingPromotion.ProductId = promotion.ProductId;
            await _context.SaveChangesAsync();
            return existingPromotion;
        }

        public async Task DeletePromotionAsync(int id)
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null)
            {
                throw new ArgumentException("Promotion not found.");
            }

            _context.Promotions.Remove(promotion);
            await _context.SaveChangesAsync();
        }

        public async Task DeletePromotionByProductIdAsync(int productId)
        {
            var promotion = await _context.Promotions.FirstOrDefaultAsync(p => p.ProductId == productId);
            if (promotion != null)
            {
                _context.Promotions.Remove(promotion);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Promotion> CreatePromotionAsync(Promotion promotion)
        {

            if (promotion == null)
            {
                throw new ArgumentNullException(nameof(promotion), "Promotion cannot be null.");
            }

            if (promotion.StartDate >= promotion.EndDate)
            {
                throw new ArgumentException("The start date must be earlier than the end date.");
            }


            await _context.Promotions.AddAsync(promotion);
            await _context.SaveChangesAsync();
            return promotion;
        }
    }
}
