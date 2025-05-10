using LavenderLine.Enums.Payment;
using NodaTime;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LavenderLine.Models
{
    public class Payment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long OrderId { get; set; }

        public string? UserId { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal RefundedAmount { get; set; } = 0;

        [Required]
        public Instant PaymentDate { get; set; } = QatarDateTime.Now;

        [Required]
        public string PaymentMethod { get; set; } = string.Empty;

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        public string PaymentIntentId { get; set; } = string.Empty;

        public Instant? RefundDate { get; set; }

        public bool IsRefunded { get; set; } = false;

        [Required]
        public virtual Order Order { get; set; } = null!;

        public virtual ApplicationUser? User { get; set; } 
    }
}
