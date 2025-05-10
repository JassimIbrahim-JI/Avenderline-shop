
using NodaTime;
using System.ComponentModel.DataAnnotations;

namespace LavenderLine.Models
{
    public class WishlistItem
    {
        public int WishlistItemId { get; set; }

        public int ProductId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        [Required]
        public Product Product { get; set; } = null!;
        public Instant? AddedDate { get; set; } = QatarDateTime.Now;

    }


}
