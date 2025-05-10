using NodaTime;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LavenderLine.Models
{
    public class Product
    {
        public int ProductId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Price { get; set; }

        // Original price is optional; if provided, it must be valid.
        [Range(0.01, double.MaxValue, ErrorMessage = "Please enter a valid original price.")]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? OriginalPrice { get; set; }

        public string? ImageUrl { get; set; }

        public Instant CreatedDate { get; set; } = QatarDateTime.Now;

        public Instant? UpdatedDate { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsFeatured { get; set; }

        public bool IsExcludedFromRelated { get; set; }


        [Required(ErrorMessage = "At least 2 lengths required.")]
        [MinLength(2, ErrorMessage = "At least 2 lengths required.")]
        public IList<string> Lengths { get; set; } = new List<string>();

        [Required(ErrorMessage = "At least 2 sizes required.")]
        [MinLength(2, ErrorMessage = "At least 2 sizes required.")]
        public IList<string> Sizes { get; set; } = new List<string>();

        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be positive.")]
        public int Quantity { get; set; }

        // Navigation Property to OrderItems
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        public int CategoryId { get; set; }

        public virtual Category Category { get; set; }
    }

}
