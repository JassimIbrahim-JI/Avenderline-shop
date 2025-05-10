using LavenderLine.Enums.NewsLetter;
using LavenderLine.VerificationServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;


namespace LavenderLine.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSenderService _emailSender;
        private readonly INotificationService _notificationService;
        IPasswordHasher<ApplicationUser> _passwordHasher;
        private readonly IOrderService _orderService;
        private readonly ICartService _cartService;
        private readonly INewsLetterService _newsLetterService;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly EcommerceContext _context;

        public AccountController(IUserRepository userRepository, UserManager<ApplicationUser> userManager, IEmailSenderService emailSender, IPasswordHasher<ApplicationUser> passwordHasher, SignInManager<ApplicationUser> signInManager, IOrderService orderService, ICartService cartService, IWebHostEnvironment env, INotificationService notificationService, INewsLetterService newsLetterService, IConfiguration configuration, EcommerceContext context)
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _emailSender = emailSender;
            _passwordHasher = passwordHasher;
            _signInManager = signInManager;
            _orderService = orderService;
            _cartService = cartService;
            _env = env;
            _notificationService = notificationService;
            _newsLetterService = newsLetterService;
            _configuration = configuration;
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string? returnUrl = null)
        {
            var model = new RegisterViewModel
            {
                ReturnUrl = returnUrl
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new
                {
                    success = false,
                    errorCode = "validation_error",
                    message = "Please fix the highlighted fields"
                });
            }

            if (await _userRepository.EmailExistsAsync(model.Email))
            {
                return Json(new
                {
                    success = false,
                    errorCode = "duplicate_email",
                    message = "Email already registered"
                });
            }

            var result = await _userRepository.CreateUserAsync(model);
            if (result.Succeeded)
            {
                var user = await _userRepository.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    // Assign a role to the user
                    var roleResult = await _userRepository.AssignRoleAsync(user, Roles.Customer);
                    if (!roleResult.Succeeded)
                    {
                        return Json(new
                        {
                            success = false,
                            message = "Failed to assign user role. Contact support."
                        });
                    }

                    var codeToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var baseUrl = _configuration["AppSettings:BaseUrl"];
                    var callbackUrl = Url.Action("ConfirmEmail", "Account",
                        new { userId = user.Id, code = codeToken }, protocol: HttpContext.Request.Scheme);

                    var emailResult = await SendEmailAsync(model.Email, callbackUrl);
                    if (!emailResult)
                    {
                        return Json(new { success = false, message = "Failed to send confirmation email. Please try again later." });
                    }
                }
                return Json(new
                {
                    success = true,
                    action = "register",
                    message = "Registration successful! Check your email",
                    redirectUrl = Url.Action("RegisterConfirmation", "Account", new { email = model.Email })
                });
            }

            return Json(new
            {
                success = false,
                errorCode = result.Errors.First().Code, // Map Identity errors
                message = string.Join(" ", result.Errors.Select(e => e.Description))
            });
        }

        public IActionResult RegisterConfirmation(string email)
        {
            ViewBag.Email = email; 
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code))
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return RedirectToAction("Index", "Home");

            var result = await _userManager.ConfirmEmailAsync(user, code);


            ViewBag.Success = result.Succeeded;

            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            LoginViewModel model = new()
            {
                ReturnUrl = returnUrl,

            };

            if (TempData["ToastMessage"] != null)
            {
                ViewData["ToastMessage"] = TempData["ToastMessage"];
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return Json(new
                {
                    success = false,
                    message = ModelState
                 .Values
                 .SelectMany(v => v.Errors)
                 .FirstOrDefault()?.ErrorMessage ?? "Invalid form submission"
                });
            }
       
            var result = await _userRepository.SignInAsync(model);

            if (result.Succeeded)
            {
                var user = await _userRepository.FindByEmailAsync(model.Email);
                var fullAddress = $"{user.Area}, {user.StreetAddress}";
                var claims = new List<Claim>()
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id),
                        new Claim("FullName", user.FullName),
                        new Claim("Address", fullAddress ?? "N/A"),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim("PhoneNumber", user.PhoneNumber),
                        new Claim(ClaimTypes.Role, user.Role)
                    };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);
               
                await _notificationService.CreateLoginNotification(user.Id);
                await HandleCartMigration();
                if(user.Role == "Admin") 
                {
                    return Json(new
                    {
                        success = true,
                        message = "Welcome back! Login successful",
                        redirectUrl = Url.Action("Index", "AdminDashboard") 
                    });
                }
                else 
                {
                return Json(new
                {
                    success = true,
                    message = "Welcome Back! Login successful",
                    redirectUrl = returnUrl ?? Url.Action("Index", "Home")
                });
                }
            }
            return Json(new
            {
                success = false,
                errorCode = "invalid_credentials",
                message = "Invalid email or password"
            });
        }

        [HttpGet, AllowAnonymous]
        public IActionResult GoogleLogin(string? returnUrl = null)
        {
            var actualReturnUrl = returnUrl ?? HttpContext.Request.Query["returnUrl"];

            var redirectUrl = Url.Action(
                nameof(ExternalLoginCallback),
                "Account",
                new { returnUrl = actualReturnUrl }
            );

            var properties = new AuthenticationProperties
            {
                RedirectUri = redirectUrl,
                Items = { { "loginProvider", "Google" } }
            };

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null)
        {
            try
            {
                // Get the Google authentication result
                var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

                // Handle authentication failure
                if (result?.Principal == null || !result.Succeeded)
                {
                    TempData["ToastError"] = "Google authentication failed";
                    return RedirectToAction(nameof(Login));
                }

                // Determine the return URL
                string actualReturnUrl;
                if (result.Properties?.Items.TryGetValue("returnUrl", out var storedReturnUrl) == true &&
                    !string.IsNullOrEmpty(storedReturnUrl))
                {
                    actualReturnUrl = storedReturnUrl;
                }
                else
                {
                    actualReturnUrl = returnUrl ?? Url.Action("Index", "Home");
                }

                // Get email from claims
                var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(email))
                {
                    TempData["ToastError"] = "Google account email not found - please ensure you've granted email access";
                    return RedirectToAction("Login");
                }

                // Check if user exists
                var user = await _userRepository.FindByEmailAsync(email);

                // Handle existing user
                if (user != null)
                {
                    await SignInUserWithGoogle(user, actualReturnUrl);
                    return RedirectToAction("Index", "Home");
                }

                // Handle new user registration
                var givenName = result.Principal.FindFirst(ClaimTypes.GivenName)?.Value;
                var surname = result.Principal.FindFirst(ClaimTypes.Surname)?.Value;
                var fullName = result.Principal.FindFirst(ClaimTypes.Name)?.Value ?? $"{givenName} {surname}";

                var creationResult = await _userRepository.CreateGoogleUserAsync(email, fullName);
                if (!creationResult.Succeeded)
                {
                    var errorMessage = string.Join(", ", creationResult.Errors.Select(e => e.Description));
              
                    TempData["ToastError"] = $"Account creation failed: {errorMessage}";
                    return RedirectToAction(nameof(Register));
                }

                // Get the newly created user and sign them in
                user = await _userRepository.FindByEmailAsync(email);
                if (user == null)
                {
                  
                    TempData["ToastError"] = "Account creation succeeded but user not found. Please try again.";
                    return RedirectToAction(nameof(Login));
                }

                await SignInUserWithGoogle(user, actualReturnUrl);
                return RedirectToAction("Index", "Home");
            }
            catch 
            {
                TempData["ToastError"] = "An error occurred during Google sign-in. Please try again.";
                return RedirectToAction(nameof(Login));
            }
        }

        private async Task SignInUserWithGoogle(ApplicationUser user, string returnUrl)
        {
            try
            {
                // First sign out any existing authentication
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                // Create claims identical to regular login
                var fullAddress = $"{user.Area}, {user.StreetAddress}";
                var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id),
                        new Claim("FullName", user.FullName),
                        new Claim("Address", fullAddress ?? "N/A"),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim("PhoneNumber", user.PhoneNumber ?? "N/A"),
                        new Claim(ClaimTypes.Role, user.Role)
                    };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                // Sign in with cookie authentication
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    claimsPrincipal,
                    new AuthenticationProperties { IsPersistent = true }
                );

                // Also sign in with the sign-in manager for compatibility
                await _signInManager.SignInAsync(user, isPersistent: true);

                // Handle any cart migration
                await HandleCartMigration();

                // Set success message
                TempData["ToastSuccess"] = user.EmailConfirmed
                    ? "Signed in with Google!"
                    : "Google registration complete!";

                // Don't need to return anything as the calling method handles redirection
            }
            catch
            {
                throw; // Rethrow to be handled by the calling method
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(string? returnUrl = null)
        {
            try
            {
                // Clear authentication and session
                await _userRepository.SignOutAsync();
                HttpContext.Session.Clear();
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                var isAdmin = User.IsInRole("Admin");
                if (isAdmin)
                {
                    returnUrl = Url.Action("Index", "Home");
                }

                else if (!Url.IsLocalUrl(returnUrl))
                {
                    returnUrl = Url.Action("Index", "Home");
                }

                // Return appropriate response based on request type
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new
                    {
                        success = true,
                        action = "logout",
                        message = "Logged out successfully",
                        redirectUrl = returnUrl
                    });
                }

                return LocalRedirect(returnUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during logout: {ex.Message}");

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new
                    {
                        success = false,
                        errorCode = "logout_error",
                        message = "Logout failed. Please try again."
                    });
                }

                ViewData["ErrorMessage"] = "Logout failed. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult ForgotPassword()
        {
            return View();
        }
       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Return validation errors as JSON
                var validationErrors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);
                return Json(new { success = false, message = string.Join(" ", validationErrors) });
            }

            var user = await _userRepository.FindByEmailAsync(model.Email);
            if (user != null && await _userManager.IsEmailConfirmedAsync(user))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var baseUrl = _configuration["AppSettings:BaseUrl"];
                var callbackUrl =Url.Action("ResetPassword", "Account",
                    new { userId = user.Id, code = token }, protocol: HttpContext.Request.Scheme);

                var emailSend = await SendResetEmailAsync(model.Email, callbackUrl);
                if (!emailSend)
                {
                    return Json(new { success = false, message = "Failed to send password reset email. Please try again." });
                }
            }

            // Always return success to avoid exposing user existence
            return Json(new
            {
                success = true,
                message = "If your email is registered, you will receive a password reset link.",
                redirectUrl = Url.Action("ResetPasswordConfirmation", "Account")
            });
        }

        [HttpGet]
        public async Task<ActionResult> ResetPassword(string userId, string? code = null)
        {
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(userId))
            {
                return View("Error"); // Handle the error case
            }

            // Optionally, you could retrieve the user to confirm they exist
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return View("Error"); // User not found
            }

            var model = new ResetPasswordViewModel
            {
                Code = code,
                UserId = user.Id
            };

            return View(model);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.UserId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                // Check if the email is confirmed  
                if (!await _userManager.IsEmailConfirmedAsync(user))
                {
                    return Json(new { success = false, message = "Email is not confirmed. Please confirm your email before resetting your password." });
                }
                var isSamePassword = await _userManager.CheckPasswordAsync(user, model.Password);
                if (isSamePassword)
                {
                    return Json(new
                    {
                        success = false,
                        message = "New password must differ from current password"
                    });
                }

                var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
                if (result.Succeeded)
                {
                    return Json(new { success = true, message = "Password reset successfully! You can now log in." });
                }

              
                var errors = result.Errors.Select(error => error.Description).ToList();
                return Json(new { success = false, message = string.Join(", ", errors) });
            }

            // Return validation errors as JSON if model state is invalid 
            var validationErrors = ModelState.Values.SelectMany(v => v.Errors).Select(v => v.ErrorMessage).ToList();
            return Json(new { success = false, message = string.Join(", ", validationErrors) });
        }

        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = User.GetUserId(HttpContext);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                ModelState.AddModelError("", "User not found.");
                return NotFound();
            }

            var orders = await _orderService.GetAllOrdersByUserIdAsync(userId);
            var model = new ProfileViewModel
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Area = user.Area ?? "",
                StreetAddress = user.StreetAddress ?? "",
                PhoneNumber = user.PhoneNumber?.Replace("+974", "") ?? "",
                Orders = orders.ToList(),
                IsSubscribed = user.IsSubscribed
            };
            ViewData["Layout"] = User.IsInRole("Admin") ? "_LayoutAdmin" : "_Layout";
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            model.PhoneNumber = model.PhoneNumber.Replace("+974", "").Trim();

            if (!Regex.IsMatch(model.PhoneNumber, @"^[3567]\d{7}$"))
            {
                ModelState.AddModelError("PhoneNumber", "Invalid Qatari mobile number");
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new { success = false, message = string.Join(", ", errors) });
            }

            try
            {
                var userId = User.GetUserId(HttpContext);
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return Json(new { success = false, message = "User not found." });

                bool wasSubscribed = user.IsSubscribed;

                user.FullName = model.FullName;
                user.Area = model.Area;
                user.StreetAddress = model.StreetAddress;
                user.PhoneNumber = $"+974{model.PhoneNumber}";
                user.IsSubscribed = model.IsSubscribed;

                if (!await _userRepository.UpdateUserAsync(user))
                    return Json(new { success = false, message = "Failed to update profile." });

                await RefreshUserClaims(user);

                if (wasSubscribed != model.IsSubscribed)
                {
                    var email = user.Email.Trim().ToLower();
                    if (model.IsSubscribed)
                    {
                        await _newsLetterService.SubscribeAsync(email);
                        var token = await _userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultProvider, "Unsubscribe");
                        var unsubscribeLink = Url.Action("Unsubscribe", "Account", new { email, token }, protocol: Request.Scheme);
                        await SendSubscribeEmailAsync(email, unsubscribeLink);
                    }
                    else
                    {
                        var token = await _userManager.GenerateUserTokenAsync(
                  user, TokenOptions.DefaultProvider, "Unsubscribe");
                        await _newsLetterService.UnsubscribeAsync(email, token);

                        var resubToken = await _userManager.GenerateUserTokenAsync(
                            user, TokenOptions.DefaultProvider, "Resubscribe");
                        var resubLink = Url.Action("Resubscribe", "Account",
                            new { email, token = resubToken },
                            Request.Scheme);

                        await SendUnsubscribeEmailAsync(email, resubLink);
                    }
                }

                    return Json(new
                    {
                        success = true,
                        fullName = user.FullName,
                        area = user.Area,
                        streetAddress = user.StreetAddress,
                        phoneNumber = user.PhoneNumber,
                        isSubscribed = user.IsSubscribed,
                        message = "Profile updated successfully!",
                        // Include redirect URL if needed
                        redirectUrl = user.IsProfileComplete && TempData.ContainsKey("ReturnToCheckout")
                ? Url.Action("Checkout", "Order")
                : null
                    });
            }
            catch
            {
                return Json(new { success = false, message = "An error occurred while updating your profile." });
            }
        }


        private async Task RefreshUserClaims(ApplicationUser user)
        {
            // 1. Grab all of the user's existing claims
            var existingClaims = await _userManager.GetClaimsAsync(user);

            // 2. Define exactly which claim types you want to keep in sync with your user entity
            var claimTypesToUpdate = new[]
            {
            ClaimTypes.NameIdentifier,
            ClaimTypes.Email,
            "FullName",
            "Address",
            "PhoneNumber"
            };
            // 3. Remove any of those claim types if they exist
            foreach (var ct in claimTypesToUpdate)
            {
                var old = existingClaims.FirstOrDefault(c => c.Type == ct);
                if (old != null)
                   await _userManager.RemoveClaimAsync(user, old);
            }

            // 4. Build the new claim values off your user object
            var newClaims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email,user.Email),
                new Claim("FullName",user.FullName),
                new Claim("Address",$"{user.Area}, {user.StreetAddress}"),
                new Claim("PhoneNumber",user.PhoneNumber)
            };
            // 5. Add them all in one go
            await _userManager.AddClaimsAsync(user, newClaims);
            // 6. Finally refresh the cookie so the updated claims take effect immediately
            await _signInManager.RefreshSignInAsync(user);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSubscription(bool isSubscribed)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Forbid();

                var email = user.Email.Trim().ToLower();
                var token = await _userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultProvider, "Unsubscribe");

                if (isSubscribed)
                {
                    await _newsLetterService.SubscribeAsync(email);
                    var link = Url.Action("Unsubscribe", "Account", new { email, token }, Request.Scheme);
                    await SendSubscribeEmailAsync(email, link);
                }
                else
                {
                    await _newsLetterService.UnsubscribeAsync(email, token);
                    var resubToken = await _userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultProvider, "Resubscribe");
                    var resubLink = Url.Action("Resubscribe", "Account", new { email, token = resubToken }, Request.Scheme);
                    await SendUnsubscribeEmailAsync(email, resubLink);
                }

                user.IsSubscribed = isSubscribed;
                await _userRepository.UpdateUserAsync(user);

                return Json(new { success = true, message = $"Subscription {(isSubscribed ? "activated" : "paused")} successfully" });
            }
            catch
            {
                return Json(new { success = false, message = "Error updating subscription. Please try again." });
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Unsubscribe(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                TempData["SubscriptionMessage"] = "Invalid unsubscribe request.";
                return RedirectToAction(nameof(SubscriptionStatus));
            }

            var result = await _newsLetterService.UnsubscribeAsync(email.Trim().ToLower(), token);

            switch (result)
            {
                case UnsubscribeResult.Success:
                    TempData["SubscriptionMessage"] = "You have been unsubscribed successfully.";
                    break;
                case UnsubscribeResult.TokenExpired:
                    TempData["SubscriptionMessage"] = "Your unsubscribe link has expired.";
                    break;
                default:
                    TempData["SubscriptionMessage"] = "Unable to unsubscribe with the provided link.";
                    break;
            }

            return RedirectToAction(nameof(SubscriptionStatus));
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Resubscribe(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                TempData["SubscriptionMessage"] = "Invalid resubscribe request.";
                return RedirectToAction(nameof(SubscriptionStatus));
            }

            // 1. Find the user by email
            var user = await _userManager.FindByEmailAsync(email.Trim().ToLower());
            if (user == null)
            {
                TempData["SubscriptionMessage"] = "User not found.";
                return RedirectToAction(nameof(SubscriptionStatus));
            }

            // 2. Verify the token
            var isValid = await _userManager.VerifyUserTokenAsync(
                user,
                TokenOptions.DefaultProvider,
                "Resubscribe",
                token);

            if (!isValid)
            {
                TempData["SubscriptionMessage"] = "Invalid or expired resubscribe link.";
                return RedirectToAction(nameof(SubscriptionStatus));
            }


            var result = await _newsLetterService.SubscribeAsync(user.Email.Trim().ToLower());
            switch (result)
            {
                case SubscriptionResult.Success:
                case SubscriptionResult.Reactivated:
                case SubscriptionResult.ReactivatedWithNewToken:
                    TempData["SubscriptionMessage"] = "You have been resubscribed successfully!";
                    break;
                default:
                    TempData["SubscriptionMessage"] = "Could not process your resubscription.";
                    break;
            }


            return RedirectToAction(nameof(SubscriptionStatus));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resubscribe()
        {
            // 1. Get the current user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Forbid();

            // 2. Mark them subscribed and persist
            user.IsSubscribed = true;
            var updated = await _userRepository.UpdateUserAsync(user);
            if (!updated)
                return Json(new { success = false, message = "Failed to update your profile. Please try again." });

            // 3. Call the newsletter service to (re)subscribe
            var email = user.Email.Trim().ToLower();
            var result = await _newsLetterService.SubscribeAsync(email);

            // 4. If successful or reactivated, fetch the subscription record for its token:
            if (result == SubscriptionResult.Success
                || result == SubscriptionResult.Reactivated
                || result == SubscriptionResult.ReactivatedWithNewToken)
            {
                // You need your EF context to read the token back out:
                var subscription = await _context.Newsletters
                    .FirstOrDefaultAsync(n => n.Email == email);

                if (subscription == null)
                    return Json(new { success = false, message = "Subscription record not found after subscribing." });

                // 5. Build the unsubscribe link and send the confirmation email
                var unsubscribeLink = Url.Action(
                    "Unsubscribe",
                    "Account",
                    new { email = subscription.Email, token = subscription.UnsubscribeToken },
                    protocol: Request.Scheme);

                var emailSent = await SendSubscribeEmailAsync(user.Email, unsubscribeLink);
                if (!emailSent)
                    return Json(new { success = false, message = "Subscribed—but we couldn't send the confirmation email. Please try again later." });

                return Json(new { success = true, message = "You’ve been resubscribed! Check your email for the unsubscribe link." });
            }

            // 6. Handle the “already exists” or other failure cases
            if (result == SubscriptionResult.AlreadyExists)
            {
                return Json(new { success = false, message = "You’re already subscribed." });
            }

            // Generic fallback
            return Json(new { success = false, message = "Could not resubscribe. Please try again." });
        }


        [HttpGet]
        [AllowAnonymous]
        public IActionResult SubscriptionStatus()
        {
            // The view will read TempData["SubscriptionMessage"]
            return View();
        }

        private async Task HandleCartMigration()
        {
            var tempUserId = HttpContext.Session.GetString("TempUserId");
            var authenticatedUserId = User.GetUserId(HttpContext);

            if (!string.IsNullOrEmpty(tempUserId) && tempUserId != authenticatedUserId)
            {
                await _cartService.MergeCartsAsync(tempUserId, authenticatedUserId);
                HttpContext.Session.Remove("TempUserId");
            }
        }

        private async Task<bool> SendEmailAsync(string email, string callbackUrl)
        {
            var subject = "Confirm your email";
            var templatePath = Path.Combine(_env.WebRootPath, "email-templates", "Confirmation.html");

            if (!System.IO.File.Exists(templatePath))
            {
               // _logger.LogError("Email template not found at: {TemplatePath}", templatePath);
                throw new FileNotFoundException("Email template not found.", templatePath);
            }

            var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";
            var logoUrl = $"{baseUrl}/svgs/avenderline-dark.svg";
            string htmlTemplate = await System.IO.File.ReadAllTextAsync(templatePath);
            string message = htmlTemplate.Replace("{ConfirmationLink}", callbackUrl).Replace("{logoUrl}", logoUrl);

            try
            {
                await _emailSender.SendEmailAsync(email, subject, message);
                return true;
            }
            catch (Exception)
            {
                //_logger.LogError(ex, "Failed to send email.");
                return false;
            }
        }

        private async Task<bool>SendSubscribeEmailAsync(string email,string callbackUrl) 
        {
            var subject = "Subscription Confirmed";
            var templatePath = Path.Combine(_env.WebRootPath, "email-templates", "SubscribeEmail.html");

            if (!System.IO.File.Exists(templatePath))
            {
                // _logger.LogError("Email template not found at: {TemplatePath}", templatePath);
                throw new FileNotFoundException("Email template not found.", templatePath);
            }
            var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";
            var logoUrl = $"{baseUrl}/svgs/avenderline-dark.svg";
            string htmlTemplate = await System.IO.File.ReadAllTextAsync(templatePath);
            string message = htmlTemplate.Replace("{UnsubscribeLink}", callbackUrl).Replace("{logoUrl}", logoUrl);

            try
            {
                await _emailSender.SendEmailAsync(email, subject, message);
                return true;
            }
            catch (Exception)
            {
                //_logger.LogError(ex, "Failed to send email.");
                return false;
            }

    }

        private async Task<bool> SendUnsubscribeEmailAsync(string email, string callbackUrl)
        {
            var subject = "Unsubscription Confirmed";
            var templatePath = Path.Combine(_env.WebRootPath, "email-templates", "UnSubscribeEmail.html");

            if (!System.IO.File.Exists(templatePath))
            {
                // _logger.LogError("Email template not found at: {TemplatePath}", templatePath);
                throw new FileNotFoundException("Email template not found.", templatePath);
            }
            var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";
            var logoUrl = $"{baseUrl}/svgs/avenderline-dark.svg";
            string htmlTemplate = await System.IO.File.ReadAllTextAsync(templatePath);
            string message = htmlTemplate.Replace("{ResubscribeLink}", callbackUrl).Replace("{logoUrl}", logoUrl);

            try
            {
                await _emailSender.SendEmailAsync(email, subject, message);
                return true;
            }
            catch (Exception)
            {
                //_logger.LogError(ex, "Failed to send email.");
                return false;
            }

        }

        private async Task<bool> SendResetEmailAsync(string email, string callbackUrl)
        {
            var subject = "Reset Your Password";
            var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";
            var logoUrl = $"{baseUrl}/svgs/avenderline-dark.svg";
            var templatePath = Path.Combine(_env.WebRootPath, "email-templates", "ForgotPasswordConfirmation.html");
            var htmlTemplate = await System.IO.File.ReadAllTextAsync(templatePath);
            var message = htmlTemplate.Replace("{ResetLink}", callbackUrl).Replace("{logoUrl}", logoUrl);

            try
            {
                await _emailSender.SendEmailAsync(email, subject, message);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
                return false;
            }
        }

    }
}
