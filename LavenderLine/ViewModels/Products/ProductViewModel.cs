
using NodaTime;
using System.ComponentModel.DataAnnotations;

namespace LavenderLine.ViewModels.Products
{
    public class ProductViewModel
    {
        public int ProductId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
        public decimal Price { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Original price must be valid.")]
        public decimal? OriginalPrice { get; set; }

        public bool IsOnSale => OriginalPrice.HasValue && OriginalPrice.Value > Price;

        public int CategoryId { get; set; }

        public string? CategoryName { get; set; }

        public string? ImageUrl { get; set; }
        public IFormFile? ImageFile { get; set; }

        public Instant CreatedDate { get; set; }

        public bool IsActive { get; set; }

        public bool IsFeatured { get; set; }

        public bool HasPromotion { get; set; }

        public bool IsExcludedFromRelated { get; set; }

        [Required(ErrorMessage = "At least 2 lengths required.")]
        public IList<string> Lengths { get; set; } = new List<string>();

        [Required(ErrorMessage = "At least 2 sizes required.")]
        public IList<string> Sizes { get; set; } = new List<string>();

        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be positive.")]
        public int Quantity { get; set; }

        public List<ProductViewModel> RelatedProducts { get; set; } = new();
    }
}
