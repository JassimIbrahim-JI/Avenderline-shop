using NodaTime;

namespace LavenderLine.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Message { get; set; } 
        public Instant CreatedAt { get; set; } = QatarDateTime.Now;
        public bool IsRead { get; set; }
        public string? RelatedId { get; set; } 
        public string? UserId { get; set; }

    }
}
