using System.ComponentModel.DataAnnotations;

namespace LavenderLine.Models
{
    public class Category
    {
        public int CategoryId { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public bool IsBanner { get; set; } = false;
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
