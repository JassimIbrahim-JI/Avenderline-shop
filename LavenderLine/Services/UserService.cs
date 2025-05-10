using LavenderLine.Validate;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace LavenderLine.Services
{

    public interface IUserService
    {
        Task<ValidateResult> UpdateUserAsync(ApplicationUser updatedUser);
        Task<ValidateResult> DeleteUserAsync(int id);
        Task<ApplicationUser> GetUserByIdAsync(string id);
        Task<IEnumerable<ApplicationUser>> GetAllUsersAsync();
        Task<(IEnumerable<ApplicationUser> Items, int TotalCount)> GetPagedUserAsync(int pageNumber, int pageSize);

    }

    public class UserService : IUserService
    {

        private readonly EcommerceContext _context;
        public UserService(EcommerceContext context)
        {
            _context = context;
        }
        public async Task<ValidateResult> UpdateUserAsync(ApplicationUser updatedUser)
        {
            // Fetch the existing user from the repository
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == updatedUser.Id);

            if (existingUser == null)
            {
                return new ValidateResult(false, "User not found.");
            }

            // Update only the fields that are provided in updatedUser (without overwriting if null)
            existingUser.FullName = updatedUser.FullName ?? existingUser.FullName;
            existingUser.Email = updatedUser.Email ?? existingUser.Email;
            existingUser.PhoneNumber = updatedUser.PhoneNumber ?? existingUser.PhoneNumber;
            existingUser.Role = updatedUser.Role ?? existingUser.Role;

            // Validate the updated user details
            var validationErrors = ValidateUser(existingUser);
            if (validationErrors.Any())
            {
                return new ValidateResult(false, string.Join(", ", validationErrors));
            }

            try
            {
                // Perform the update
                await _context.SaveChangesAsync();
                return new ValidateResult(true, "User updated successfully.");
            }
            catch (Exception ex)
            {
                // Optionally log the exception here
                return new ValidateResult(false, $"An error occurred while updating the user: {ex.Message}");
            }
        }
        public async Task<ValidateResult> DeleteUserAsync(int id)
        {
            // Find the user by ID
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return new ValidateResult(false, "User not found.");
            }

            try
            {
                // Remove the user from the database
                _context.Users.Remove(user);

                // Save changes asynchronously
                await _context.SaveChangesAsync();

                return new ValidateResult(true, "User deleted successfully.");
            }
            catch (Exception ex)
            {
                // Optionally log the exception
                return new ValidateResult(false, $"An error occurred while deleting the user: {ex.Message}");
            }
        }


        public async Task<ApplicationUser> GetUserByIdAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<IEnumerable<ApplicationUser>> GetAllUsersAsync()
        {
            var users = await _context.Users.ToListAsync();
            return users;
        }
        public async Task<(IEnumerable<ApplicationUser> Items, int TotalCount)> GetPagedUserAsync(int pageNumber, int pageSize)
        {
            // Ensure pageNumber is at least 1
            // (avoid negative/zero page numbers)
            pageNumber = pageNumber < 1 ? 1 : pageNumber;

            var totalCount = await _context.Users.CountAsync();
            var items = await _context.Users.Skip((pageNumber - 1) * pageSize)
             .Take(pageSize)
             .ToListAsync();
            return (items, totalCount);

        }

        private List<string> ValidateUser(ApplicationUser user)
        {
            var errors = new List<string>();

            // Validate First Name
            if (string.IsNullOrWhiteSpace(user.FullName))
            {
                errors.Add("First name is required.");
            }

            // Validate Email
            if (string.IsNullOrWhiteSpace(user.Email) || !new EmailAddressAttribute().IsValid(user.Email))
            {
                errors.Add("Invalid email address.");
            }

            // Validate Phone Number
            if (string.IsNullOrWhiteSpace(user.PhoneNumber) || !new PhoneAttribute().IsValid(user.PhoneNumber))
            {
                errors.Add("Invalid phone number format.");
            }

            // Validate Role
            if (string.IsNullOrWhiteSpace(user.Role) ||
                !(user.Role == Roles.Manager || user.Role == Roles.Customer || user.Role == Roles.Admin))
            {
                errors.Add("Invalid role specified. Valid roles are: Manager, Customer, Admin.");
            }

            return errors;
        }


    }
}
