using NodaTime;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LavenderLine.Models
{
    public class NewsletterSubscription
    {
        public int Id { get; set; }
        public string? Email { get; set; } = string.Empty;
        public Instant SubscribedOn { get; set; }
        public Instant? UnsubscribedOn { get; set; }

        [Required]
        [MaxLength(100)]
        public string UnsubscribeToken { get; set; } = GenerateNewToken();

        [Required]
        public Instant TokenExpiration { get; set; } = QatarDateTime.Now.Plus(Duration.FromDays(90));

        public bool IsActive { get; set; } = true;

        // Computed properties
        [NotMapped]
        public LocalDateTime SubscribedOnQatar =>
            SubscribedOn.InZone(QatarDateTime.QatarZone).LocalDateTime;

        [NotMapped]
        public bool IsTokenValid =>
            QatarDateTime.Now < TokenExpiration;

        public static string GenerateNewToken() =>
            Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
    }

}
