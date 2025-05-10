
namespace LavenderLine.ViewModels.Wishlists
{
    public class WishlistItemViewModel
    {
        public int ProductId { get; set; }
        public Product Product { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? OriginalPrice { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string Category { get; set; }

        public int Quantity { get; set; }
        public bool IsFavorite { get; set; }
    }
}
