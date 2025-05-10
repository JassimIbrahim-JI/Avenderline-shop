using System.ComponentModel.DataAnnotations;

namespace LavenderLine.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "Password must be at least {2} characters long.", MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare(nameof(Password), ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;


        public string? ReturnUrl { get; set; }

        [Required(ErrorMessage = "Qatari phone number required")]
        [RegularExpression(@"^(\+974\d{8}|[3567]\d{7})$",
           ErrorMessage = "Invalid Qatari number format")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Display(Name = "User Type")]
        public string UserType { get; set; } = Roles.Customer;


    }
}