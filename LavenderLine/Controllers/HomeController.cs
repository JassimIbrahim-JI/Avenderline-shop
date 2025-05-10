using LavenderLine.Enums.NewsLetter;
using LavenderLine.ViewModels.Products;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace LavenderLine.Controllers
{
    public class HomeController : Controller
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IInstagramPostService _instagramService;
        private readonly IPromotionService _promotionService;
        private readonly INewsLetterService _newsletterService;
        private readonly IEmailSenderService _emailService;
        private readonly ICartService _cartService;
        private readonly IAntiforgery _antiforgery;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        private readonly EcommerceContext _context;


        public HomeController(IProductService productService, ICategoryService categoryService,
            IInstagramPostService instagramService, IPromotionService promotionService, ICartService cartService, IAntiforgery antiforgery, INewsLetterService newsletterService, IEmailSenderService emailService, IWebHostEnvironment env, IConfiguration config, EcommerceContext context)
        {
            _productService = productService;
            _categoryService = categoryService;
            _instagramService = instagramService;
            _promotionService = promotionService;
            _cartService = cartService;
            _antiforgery = antiforgery;
            _newsletterService = newsletterService;
            _emailService = emailService;
            _env = env;
            _config = config;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["ActiveLink"] = "Index";
          

            var userId = User.GetUserId(HttpContext);

            ViewBag.UserId = userId;

            var banners = await _categoryService.GetBannerCategoriesAsync();

            var bannerProducts = new Dictionary<int, List<ProductViewModel>>();
            foreach (var banner in banners)
            {
                var products = await _productService.GetProductsByCategoryAsync(banner.CategoryId);
                bannerProducts[banner.CategoryId] = products.Select(p => new ProductViewModel
                {
                    ProductId = p.ProductId,
                    Name = p.Name,
                    Price = p.Price,
                    OriginalPrice = p.OriginalPrice,
                    ImageUrl = p.ImageUrl,
                    Quantity = p.Quantity,
                    CategoryName = p.Category?.Name ?? "Uncategorized",
                    Sizes = p.Sizes,
                    Lengths = p.Lengths
                   
                }).ToList();
            }

            // Prepare the HomeViewModel
            var viewModel = new HomeViewModel
            {
                FeaturedProducts = await _productService.GetFeaturedProductAsync(),

                CartItems = await _cartService.GetCartAsync(userId),
                CurrentPromotion = await _promotionService.GetCurrentPromotionAsync(),

                ActiveProducts = await _productService.GetActiveProducts(),

                // Calculate discountPercentage based on active products
                DiscountPercentage = CalculateDiscountPercentage(await _productService.GetActiveProducts()),


                Categories = await _categoryService.GetAllCategoriesAsync(),
                InstagramPosts = await _instagramService.GetAllPostsAsync(),
                Banners = banners,
                BannerProducts = bannerProducts
            };
            return View(viewModel);
        }

        private decimal CalculateDiscountPercentage(IEnumerable<ProductViewModel> products)
        {
            if (!products.Any()) return 0;

            decimal totalDiscount = products
                .Where(p => p.OriginalPrice.HasValue && p.OriginalPrice > p.Price)
                .Sum(p => p.OriginalPrice.Value - p.Price);

            decimal totalOriginalPrice = products
                .Where(p => p.OriginalPrice.HasValue)
                .Sum(p => p.OriginalPrice.Value);

            return totalOriginalPrice > 0
                ? Math.Round((totalDiscount / totalOriginalPrice) * 100, 2)
                : 0;
        }
        public IActionResult Privacy()
        {
            ViewData["ActiveLink"] = "Privacy";
            return View();
        }

        public async Task<IActionResult> Shop()
        {

            ViewData["ActiveLink"] = "Shop";
            var activeProducts = await _productService.GetActiveProducts();
            var categories = await _categoryService.GetAllCategoriesAsync();
            ViewBag.Categories = categories;
            return View(activeProducts);
        }
        [HttpGet]
        public async Task<IActionResult> GetPriceRange(string category)
        {
            var prices = await _productService.GetPriceRangeAsync(category);
            return Json(new
            {
                min = (double)prices.MinPrice,
                max = (double)prices.MaxPrice
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetFilteredProducts(
            string category,
            decimal minPrice,
            decimal maxPrice,
            string sortBy,
            int page,
            int pageSize,
            bool showExcludedOnly = false)
        {
            var result = await _productService.GetFilteredProductsAsync(
                category,
                minPrice,
                maxPrice,
                sortBy,
                page,
                pageSize,
                showExcludedOnly
            );
            return Json(new { products = result.Products, totalCount = result.TotalCount });
        }

        public async Task<IActionResult> Arrivals()
        {
            ViewData["ActiveLink"] = "Arrivals";
            // categoryName = null → all categories
            // currentProductId = 0 → skip the “exclude this” check
            // maxItems = 0 → no Take(), i.e. return everything
            var newArrivalsProducts = await _productService
                .GetRelatedProductsAsync(
                   categoryName: null,
                   currentProductId: 0,
                   maxItems: 0,
                   true
                );

            var categories = await _categoryService.GetAllCategoriesAsync();
            ViewBag.Categories = categories;
            return View(newArrivalsProducts);
        }
        public async Task<IActionResult> Details(int id)
        {

            var product = await _productService.GetProductByIdWithCategoryAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            var userId = User.GetUserId(HttpContext);

            ViewBag.UserId = userId;

            if (product == null) return NotFound();

            var viewModel = new ProductViewModel
            {
                ProductId = product.ProductId,
                Name = product.Name,
                Price = product.Price,
                OriginalPrice = product.OriginalPrice,
                Description = product.Description,
                CategoryName = product.Category?.Name,
                ImageUrl = product.ImageUrl,
                Quantity = product.Quantity,
                Lengths = product.Lengths.Select(c => c.Trim()).ToList(),
                Sizes = product.Sizes.Select(s => s.Trim()).ToList(),
                RelatedProducts = await _productService.GetRelatedProductsAsync(product.Category?.Name ?? string.Empty, 0,8,showExcludedOnly:true)
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Search query is required");

            var products = await _productService.SearchProductsAsync(query);

            return Ok(new
            {
                Products = products
            });
        }

        public IActionResult ChangeLanguage(string culture, string returnUrl)
        {
            // Validate culture parameter
            culture = culture?.ToLower() switch
            {
                "ar" => "ar",
                _ => "en" // default to English
            };

            // Set cookie with secure options
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture, culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    Secure = Request.IsHttps, // Only send over HTTPS
                    HttpOnly = true, // Prevent client-side access
                    SameSite = SameSiteMode.Lax,
                    Path = "/"
                }
            );

            // Safe redirect
            return RedirectToLocal(returnUrl);
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            return Url.IsLocalUrl(returnUrl)
                ? Redirect(returnUrl)
                : RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubscribeNonLoggedIn(string email)
        {
            try
            {
                if (!IsValidEmail(email))
                    return Json(new { success = false, message = "Invalid email format" });

                var result = await _newsletterService.SubscribeAsync(email);

                return result switch
                {
                    SubscriptionResult.Success
                        => await HandleNewSubscription(email),

                    SubscriptionResult.Reactivated
                    or SubscriptionResult.ReactivatedWithNewToken
                        => Json(new
                        {
                            success = true,
                            isResubscribe = true,
                            message = "Welcome back! You have been resubscribed."
                        }),

                    SubscriptionResult.AlreadyExists
                        => Json(new { success = false, message = "Already subscribed" }),

                    _ => Json(new { success = true, message = "Subscription refreshed" })
                };
            }
            catch
            {
                return Json(new { success = false, message = "Subscription failed" });
            }
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var pattern = @"^[a-zA-Z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-zA-Z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?\.)+[a-zA-Z]{2,}$";
                return Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        private async Task<IActionResult> HandleNewSubscription(string email)
        {
            // fetch the newly-created subscription so we can get its token
            var subscription = await _context.Newsletters
                .FirstAsync(n => n.Email == email.Trim().ToLower());

            var unsubscribeLink =Url.Action("Unsubscribe", "Home", new
            {
                email = subscription.Email,
                token = subscription.UnsubscribeToken
            }, protocol: Request.Scheme);

            var privacyPage = Url.Action("Privacy", "Home", null, protocol: Request.Scheme);

            await SendSubscribeEmailAsync(email, unsubscribeLink,privacyPage);

            return Json(new { success = true, message = "Subscription confirmed - check your email" });
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Unsubscribe(string email, string token)
        {
            var result = await _newsletterService.UnsubscribeAsync(email, token);

            TempData["SubscriptionMessage"] = result switch
            {
                UnsubscribeResult.Success => "You have been unsubscribed successfully.",
                UnsubscribeResult.TokenExpired => "Your unsubscribe link has expired.",
                _ => "Unable to unsubscribe with the provided link."
            };

            // Redirect to the shared status page in AccountController:
            return RedirectToAction("SubscriptionStatus", "Account");
        }



        private async Task<bool> SendSubscribeEmailAsync(string email, string callbackUrl,string privacyPage)
        {
            try
            {
                var subject = "Subscription Confirmed";
                string message;

                // Try to get the email template
                var templatePath = Path.Combine(_env.WebRootPath, "email-templates", "SubscribeEmail.html");

                if (System.IO.File.Exists(templatePath))
                {
                    // Template exists, use it
                    string htmlTemplate = await System.IO.File.ReadAllTextAsync(templatePath);
                    var logoUrl = "~/svgs/avenderline-dark.svg";

                    message = htmlTemplate.Replace("{UnsubscribeLink}", callbackUrl).Replace("{logoUrl}",logoUrl).Replace("{privacyPage}",privacyPage);
                }
                else
                {
                    // Template doesn't exist, use a fallback
                    message = $@"
                <html>
                <body>
                    <h2>Thank you for subscribing!</h2>
                    <p>You have successfully subscribed to our newsletter.</p>
                    <p>If you wish to unsubscribe, <a href='{callbackUrl}'>click here</a>.</p>
                </body>
                </html>";
                }

                await _emailService.SendEmailAsync(email, subject, message);
                return true;
            }
            catch (Exception ex)
            {
                // Log the exception
                return false;
            }
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


    }


}
