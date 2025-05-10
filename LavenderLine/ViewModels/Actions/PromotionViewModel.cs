using NodaTime;

namespace LavenderLine.ViewModels.Actions
{
    public class PromotionViewModel
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal DiscountPercentage { get; set; }
        public Instant EndDate { get; set; }
        public string ImageUrl { get; set; }
        public List<string> Badges { get; set; } = new List<string>();
        public string ProductName { get; set; }
        public string ProductImageUrl { get; set; }
        public string Category { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal DiscountedPrice => OriginalPrice - (OriginalPrice * DiscountPercentage / 100);
    }
}
