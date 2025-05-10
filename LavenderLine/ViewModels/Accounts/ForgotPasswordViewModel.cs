using System.ComponentModel.DataAnnotations;

namespace LavenderLine.ViewModels.Accounts
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
