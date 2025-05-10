using Microsoft.AspNetCore.Identity;
using NodaTime;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LavenderLine.Data
{
    public class ApplicationUser : IdentityUser
    {

        [Display(Name = "Full Name")]
        public string? FullName { get; set; } = string.Empty;

        public string? Area { get; set; } = string.Empty;

        [StringLength(200, MinimumLength = 5)]
        public string? StreetAddress { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsSubscribed { get; set; } = false;
        public Instant CreatedAt { get; set; } = QatarDateTime.Now;


        // Computed Qatar local time (not mapped to database)
        [NotMapped]
    public LocalDateTime CreatedAtQatar => 
        CreatedAt.InZone(QatarDateTime.QatarZone).LocalDateTime;


        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<WishlistItem> Wishlists { get; set; } = new List<WishlistItem>();

        [NotMapped]
        public bool IsProfileComplete =>
      !string.IsNullOrEmpty(FullName) &&
      !string.IsNullOrEmpty(Area) &&
      !string.IsNullOrEmpty(StreetAddress) &&
      !string.IsNullOrEmpty(PhoneNumber);

    }
}
