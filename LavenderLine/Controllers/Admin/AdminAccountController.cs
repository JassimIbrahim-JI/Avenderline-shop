using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LavenderLine.Controllers.Admin
{
    [Authorize(Policy = "RequireManagerOrAdminRole")]

    public class AdminAccountController : Controller
    {
        private readonly IUserService _userService;

        public AdminAccountController(IUserService userService)
        {
            _userService = userService;
        }

        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10)
        {
            var (users, totalCount) = await _userService.GetPagedUserAsync(pageNumber, pageSize);
            ViewBag.ActiveLink = "Users";
            var userViewModels = users.Select(u => new UserViewModel
            {
                Id = u.Id,
                Email = u.Email,
                FullName = u.FullName,
                Area = u.Area,
                StreetAddress = u.StreetAddress,
                PhoneNumber = u.PhoneNumber,
                Role = u.Role,

            }).ToList();


            ViewBag.TotalCount = totalCount; // Set the total count for pagination
            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;

            return View(userViewModels);
        }

        // GET: admin/accounts/edit/{id}
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest("User ID is null or empty.");

            ViewBag.ActiveLink = "Users";
            ViewBag.ActiveAction = "Edit";
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return NotFound();
            }

            var userViewModel = new UserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Area = user.Area,
                StreetAddress = user.StreetAddress,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,

            };
            return View(userViewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest("User ID is null or empty.");

            ViewBag.ActiveLink = "Users";
            ViewBag.ActiveAction = "Details";
            // Fetch the user by ID
            var user = await _userService.GetUserByIdAsync(id); // Ensure this method retrieves the user without eager loading

            if (user == null)
            {
                return NotFound();
            }

            // Optionally, load Payments and Orders if lazy loading is not enabled
            // await _context.Entry(user).Collection(u => u.Payments).LoadAsync();
            // await _context.Entry(user).Collection(u => u.Orders).LoadAsync();
        
            var userViewModel = new UserViewModel
            {
                Id = user.Id,
                Email = user.Email!,
                FullName = user.FullName ?? string.Empty,  
                Area = user.Area ?? string.Empty,    
                StreetAddress = user.StreetAddress ?? string.Empty,    
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Role = user.Role,
                Payments = user.Payments?.ToList() ?? new List<Payment>(),
                Orders = user.Orders?.ToList() ?? new List<Order>() 

            };

            ViewData["CurrentController"] = "AdminAccount";
            ViewData["CurrentAction"] = "Details";

            return View(userViewModel);
        }


        [HttpPost]
        public async Task<IActionResult> Edit(UserViewModel userViewModel)
        {
            // Validate the model state
            if (!ModelState.IsValid)
            {
                return View(userViewModel); // Return the view with validation errors
            }

            // Map UserViewModel to ApplicationUser
            var user = new ApplicationUser
            {
                Id = userViewModel.Id,
                Email = userViewModel.Email,
                FullName = userViewModel.FullName,
                Area = userViewModel.Area,
                StreetAddress = userViewModel.StreetAddress,
                PhoneNumber = userViewModel.PhoneNumber,
                Role = userViewModel.Role
            };

            // Attempt to update the user
            ViewData["CurrentController"] = "AdminAccount";
            ViewData["CurrentAction"] = "Edit";
            var result = await _userService.UpdateUserAsync(user);
            if (!result.isValid)
            {
                TempData["ErrorMessage"] = "User failed to update!";
                ModelState.AddModelError(string.Empty, result.message); // Add error to ModelState
                return View(userViewModel); // Return the view with the user's input and error messages
            }

            // Success case
            TempData["SuccessMessage"] = "User updated successfully!";
            return RedirectToAction("Index", new { pageNumber = 1 });
        }

        // GET: admin/accounts/delete/{id}
        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var userViewModel = new UserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Area = user.Area,
                StreetAddress = user.StreetAddress,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role
            };

            return View(userViewModel);
        }

        // POST: admin/accounts/delete
        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await _userService.DeleteUserAsync(id);
            if (!result.isValid)
            {
                ModelState.AddModelError(string.Empty, result.message);
                return RedirectToAction("Index");
            }

            return RedirectToAction("Index");
        }
    }
}
