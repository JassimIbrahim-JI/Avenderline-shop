using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LavenderLine.ViewModels.Orders
{
    public class CheckoutViewModel
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select your area")]
        public string Area { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address details are required")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Address must be between 5-200 characters")]
        public string AddressLine { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^\d{8}$", ErrorMessage = "Please enter 8 digits for your phone number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public DateTime? DeliveryDate { get; set; }

        [Required]
        public string PaymentMethod { get; set; } = string.Empty;

        public List<CartItemDto> CartItems { get; set; } = new List<CartItemDto>();

        public decimal ShippingFee { get; set; } = 50.00m; 
        public decimal CartTotal => CartItems.Sum(item => item.Price * item.Quantity);
        public decimal TotalAmount { get; set; }

        [NotMapped]
        public decimal CalculatedTotal => CartTotal + ShippingFee;


    }
}
