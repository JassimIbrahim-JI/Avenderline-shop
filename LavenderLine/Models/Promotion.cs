using NodaTime;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LavenderLine.Models
{
    public class Promotion
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string PromotionText { get; set; } = string.Empty;

      
        [Column(TypeName = "decimal(18, 0)")]
        public decimal DiscountPercentage { get; set; }

        public Instant StartDate { get; set; } = QatarDateTime.Now;

        public Instant EndDate { get; set; } = QatarDateTime.Now;

        // Always required
        public int ProductId { get; set; }

        // Product is optional during creation.
        public Product? Product { get; set; }

        [Required]
        public string Category { get; set; } = string.Empty;
    }

}