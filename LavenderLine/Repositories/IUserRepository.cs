using Microsoft.AspNetCore.Identity;

namespace LavenderLine.Repositories
{
    public interface IUserRepository
    {
        Task<IdentityResult> CreateUserAsync(RegisterViewModel model);
        Task<ApplicationUser?> FindByEmailAsync(string email);
        Task<IdentityResult> AssignRoleAsync(ApplicationUser user, string role);
        Task<SignInResult> SignInAsync(LoginViewModel model);
        Task SignOutAsync();
        Task<IdentityResult> ValidateUserRegistrationAsync(RegisterViewModel model);
        Task<IdentityResult> ForgotPasswordAsync(string email);
        Task<IdentityResult> ResetPasswordAsync(string userId, string token, string newPassword);
        Task<bool> UpdateUserAsync(ApplicationUser user);
        Task<IdentityResult> SubscribeNewsletterAsync(string email);
        Task<bool> EmailExistsAsync(string email);
        Task<IdentityResult> CreateGoogleUserAsync(string email,string fullName);


    }
}
