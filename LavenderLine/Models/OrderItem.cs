using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LavenderLine.Models
{
    public class OrderItem
    {
        [Key]
        public int OrderItemId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public long OrderId { get; set; }

        [Required(ErrorMessage = "Length is required.")]
        public string Length { get; set; } = string.Empty;

        [Required(ErrorMessage = "Size is required.")]
        public string Size { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Price { get; set; }

        public string? SpecialRequest { get; set; } = string.Empty;

        // Navigation properties
        public virtual Product? Product { get; set; }
        public virtual Order? Order { get; set; }
    }
}
