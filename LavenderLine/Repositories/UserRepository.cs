using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace LavenderLine.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public UserRepository(UserManager<ApplicationUser> manager, SignInManager<ApplicationUser> signInManager, RoleManager<IdentityRole> roleManager, IHttpContextAccessor httpContextAccessor)
        {
            _userManager = manager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IdentityResult> AssignRoleAsync(ApplicationUser user, string role)
        {
            var existingUser = await _userManager.FindByIdAsync(user.Id);
            if (existingUser == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = $"User with ID {user.Id} does not exist." });
            }

            var roleExist = await _roleManager.RoleExistsAsync(role);
            if (!roleExist)
            {
                return IdentityResult.Failed(new IdentityError { Description = $"Role {role} does not exist." });
            }
            // Assign the role to the user
            return await _userManager.AddToRoleAsync(user, role);
        }

        public async Task<IdentityResult> CreateUserAsync(RegisterViewModel model)
        {
            var validationResult = await ValidateUserRegistrationAsync(model);
            if (!validationResult.Succeeded)
            {
                return validationResult;
            }

            if (!model.PhoneNumber.StartsWith("+974") && model.PhoneNumber.Length == 8)
            {
                model.PhoneNumber = $"+974{model.PhoneNumber}";
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
/*                FullName = model.FullName,
                Address = model.Address,*/
                PhoneNumber = model.PhoneNumber,
                Role = Roles.Customer
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
               //  await _signInManager.SignInAsync(user, isPersistent: false);
               /* await _userManager.AddClaimAsync(user, new Claim("FullName", model.FullName));
                await _userManager.AddClaimAsync(user, new Claim("Address", model.Address));*/
            }
            return result;
        }

        public async Task<ApplicationUser?> FindByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<SignInResult> SignInAsync(LoginViewModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            // Find the user by email
            var user = await FindByEmailAsync(model.Email);
            if (user == null)
            {
                return SignInResult.Failed; // User does not exist
            }

            // Validate the password
            var passwordValid = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!passwordValid)
            {
                return SignInResult.Failed; // Invalid password
            }

            // Attempt to sign in the user
            var result = await _signInManager.PasswordSignInAsync(
                user, // Use the existing user object
                model.Password,
                isPersistent: model.RememberMe,
                lockoutOnFailure: false
            );
            return result;
        }

        public async Task SignOutAsync()
        {
            await _signInManager.SignOutAsync();
        }

        public async Task<IdentityResult> ValidateUserRegistrationAsync(RegisterViewModel model)
        {
            var errors = new List<IdentityError>();

            var existingUser = await FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                errors.Add(new IdentityError { Description = "Email is already in use." });
            }

            return errors.Count > 0
                ? IdentityResult.Failed(errors.ToArray())
                : IdentityResult.Success;
        }
        public async Task<IdentityResult> ResetPasswordAsync(string userId, string token, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found." });

            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            return result;
        }

        public async Task<IdentityResult> ForgotPasswordAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return IdentityResult.Success; // No action taken
            }

            if (!await _userManager.IsEmailConfirmedAsync(user))
            {

                throw new InvalidOperationException("Email is not confirmed. Please confirm your email before resetting your password.");
            }

            return IdentityResult.Success;
        }

        public async Task<bool> UpdateUserAsync(ApplicationUser user)
        {
            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }
       
        public async Task<IdentityResult> SubscribeNewsletterAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "User not found." });
            }

            // Check if the user is already subscribed.
            if (user.IsSubscribed)
            {
                return IdentityResult.Failed(new IdentityError { Description = "User is already subscribed." });
            }

            // Mark the user as subscribed.
            user.IsSubscribed = true;
            return await _userManager.UpdateAsync(user);
        }
        public async Task<bool> EmailExistsAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            return user != null;
        }

        public async Task<IdentityResult> CreateGoogleUserAsync(string email, string fullName)
        {
          
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                Area = "N/A", 
                StreetAddress = "N/A",
                PhoneNumber = "N/A",
                Role = Roles.Customer,
                CreatedAt = QatarDateTime.Now
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded) return createResult;

            // Ensure role exists
            if (!await _roleManager.RoleExistsAsync(Roles.Customer))
                await _roleManager.CreateAsync(new IdentityRole(Roles.Customer));

            return await _userManager.AddToRoleAsync(user, Roles.Customer);
        }

    }
}
