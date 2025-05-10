using LavenderLine.Enums.Order;
using NodaTime;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LavenderLine.Models
{
    public class Order
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long OrderId { get; set; }

        // Will be null if the order is from a guest
        public string? UserId { get; set; }

        public Instant OrderDate { get; set; } = QatarDateTime.Now;

        public Instant? DeliveryDate { get; set; }

     
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalAmount { get; set; }

        public virtual ApplicationUser? User { get; set; }

        // Collection of order items
        public virtual List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        public virtual List<OrderHistory> OrderHistories { get; set; } = new List<OrderHistory>();
        public long? PaymentId { get; set; }
        public virtual Payment? Payment { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public Instant? ModifiedDate { get; set; }

        [StringLength(100)]
        public string? GuestFullName { get; set; }

        [StringLength(200)]
        public string? GuestAddress { get; set; }

        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string? GuestEmail { get; set; }

        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        public string? GuestPhoneNumber { get; set; }

        public string? GuestToken { get; set; }

        [Column(TypeName = "decimal(5, 2)")]
        public decimal ShippingFee { get; set; } = 50.00m;

        /// <summary>
        /// Calculates the total amount based on the order items.
        /// </summary>
        public void CalculateTotalAmount()
        {
            TotalAmount = OrderItems.Sum(item => item.Price * item.Quantity) + ShippingFee;
        }

        public override string ToString()
        {
            return $"Order ID: {OrderId}, User: {UserId}, Total Amount: {TotalAmount:C}, Status: {Status}";
        }
    }

}
