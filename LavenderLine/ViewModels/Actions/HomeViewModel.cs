using LavenderLine.ViewModels.Products;

namespace LavenderLine.ViewModels.Actions
{
    public class HomeViewModel
    {
        public IEnumerable<ProductViewModel> ActiveProducts { get; set; } = new List<ProductViewModel>();
        public IEnumerable<ProductViewModel> FeaturedProducts { get; set; } = new List<ProductViewModel>();
        public CartDto CartItems { get; set; }
        public Promotion CurrentPromotion { get; set; }
        public decimal DiscountPercentage { get; set; }
        public IEnumerable<Category> Categories { get; set; } = new List<Category>();
        public IEnumerable<InstagramPost> InstagramPosts { get; set; } = new List<InstagramPost>();
        public List<Category> Banners { get; set; }
        public Dictionary<int, List<ProductViewModel>> BannerProducts { get; set; } = new Dictionary<int, List<ProductViewModel>>();
    }
}
