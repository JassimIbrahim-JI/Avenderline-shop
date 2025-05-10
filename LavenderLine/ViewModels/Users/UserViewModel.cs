using System.ComponentModel.DataAnnotations;

namespace LavenderLine.ViewModels.Users
{
    public class UserViewModel
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "Full Name is required.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Area is required.")]
        public string Area { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200, MinimumLength = 5)]
        public string StreetAddress { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number.")]
        [RegularExpression(@"^\d{8}$", ErrorMessage = "Please enter 8 digits for your phone number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role is required.")]
        [ValidRole]
        public string Role { get; set; } = string.Empty;
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    }
}
