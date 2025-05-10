using System.ComponentModel.DataAnnotations;

namespace LavenderLine.ViewModels.Accounts
{
    public class VerifyPhoneViewModel
    {

        [Required]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Verification Code")]
        public string VerificationCode { get; set; } = string.Empty;
    }
}
