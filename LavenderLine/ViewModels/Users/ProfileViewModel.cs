using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace LavenderLine.ViewModels.Users
{
    public class ProfileViewModel
    {
        public string? UserId { get; set; }

        public string? Email { get; set; }

        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Area selection is required")]
        public string Area { get; set; }

        [Required(ErrorMessage = "Street address required")]
        [StringLength(200, MinimumLength = 5)]
        public string StreetAddress { get; set; }

        [Required(ErrorMessage = "Qatari phone number required")]
        [RegularExpression(@"^\d{8}$", ErrorMessage = "Please enter 8 digits for your phone number")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Subscribe to Newsletter")]
        public bool IsSubscribed { get; set; }

        [ValidateNever]
        public List<Order> Orders { get; set; } = new List<Order>();

    }
}
