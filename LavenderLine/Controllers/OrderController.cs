using LavenderLine.Enums.Order;
using LavenderLine.Enums.Payment;
using LavenderLine.VerificationServices;
using LavenderLine.ViewModels.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NodaTime;
using NodaTime.TimeZones;
using System.Globalization;


namespace LavenderLine.Controllers
{
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ICartService _cartService;
        private readonly IPaymentService _paymentService;
        private readonly IPaymentGatewayService _fatoorahService;
        private readonly IConfiguration _configuration;
      
        private readonly IUserService _userService;
        private readonly IEmailSenderService _emailSenderService;
        private readonly INotificationService _notificationService;
        private readonly EcommerceContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<OrderController> _logger;

        public OrderController(
            IOrderService orderService,
            ICartService cartService,
            IPaymentService paymentService,
            EcommerceContext context
,
            IUserService userService,
            IEmailSenderService emailSenderService,
            IPaymentGatewayService stripeService,
            IConfiguration configuration,
            IWebHostEnvironment env,
            INotificationService notificationService,
            ILogger<OrderController> logger)
        {
            _orderService = orderService;
            _cartService = cartService;
            _paymentService = paymentService;
            _context = context;
            _userService = userService;
            _emailSenderService = emailSenderService;
            _fatoorahService = stripeService;
            _configuration = configuration;
            _env = env;
          
            _notificationService = notificationService;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Confirmation(long orderId, string? guestToken = null)
        {
            try
            {
                var order = await _orderService.GetOrderByIdAsync(orderId);
                if (order == null)
                {
                    _logger.LogWarning("Confirmation requested for non-existent order: {OrderId}", orderId);
                    return NotFound();
                }
                if (!(User.Identity?.IsAuthenticated ?? false))
                {
                    if (string.IsNullOrEmpty(guestToken) || order.GuestToken != guestToken)
                    {
                        _logger.LogWarning("Invalid guest token for order {OrderId}", orderId);
                        return NotFound();
                    }
                }
                else if (order.UserId != User.GetUserId(HttpContext))
                {
                    _logger.LogWarning("User {UserId} attempted to access order {OrderId} belonging to {OrderUserId}",
                       User.GetUserId(HttpContext), orderId, order.UserId);
                    return NotFound();
                }

                return View(order);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error loading confirmation for order {OrderId}", orderId);
                return RedirectToAction("Error", "Home");
            }
         
        }

        [HttpGet]
        public async Task<IActionResult> GetNotificationCount()
        {
            var userId = User.GetUserId(HttpContext);
            var pendingOrders = await _orderService.GetOrdersByUserIdAndStatusAsync(userId, OrderStatus.Pending);
            return Json(new { count = pendingOrders.Count() });
        }

        [HttpPost]
        public async Task<IActionResult> ClearNotification()
        {
            var userId = User.GetUserId(HttpContext);
            var pendingOrders = await _orderService.GetOrdersByUserIdAndStatusAsync(userId, OrderStatus.Pending);


            foreach (var order in pendingOrders)
            {
                order.Status = OrderStatus.Processing;
                _context.Orders.Update(order);
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> Index(
    int page = 1,
    int pageSize = 10,
    OrderStatus? status = null,
    string search = "")
        {
            var userId = User.GetUserId(HttpContext);
            var isAdmin = User.IsInRole("Admin");

            var (orders, totalCount) = await _orderService.GetOrdersAsync(
                userId: isAdmin ? null : userId,
                status: status,
                search: search,
                page: page,
                pageSize: pageSize);

            var vm = new OrderIndexViewModel
            {
                Orders = orders,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                SelectedStatus = status,
                SearchQuery = search
            };

            if (User.IsInRole("Admin"))
            {
                ViewData["Layout"] = "_LayoutAdmin";
            }

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.GetUserId(HttpContext);
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null || order.UserId != userId)
            {
                return NotFound();
            }

            if (User.IsInRole("Admin"))
            {
                ViewData["Layout"] = "_LayoutAdmin";
            }
            else
            {
                ViewData["Layout"] = "_Layout";
            }

            return View(order);
        }

       
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Checkout()
        {
            var userId = User.GetUserId(HttpContext);
            var cartItems = await _cartService.GetCartAsync(userId);

            if (!cartItems.Items.Any())
            {
                return RedirectToAction("Index", "Cart");
            }
            var cartTotal = cartItems.Items.Sum(item => item.Price * item.Quantity);
            var shippingFee = 50.00m;

            var model = new CheckoutViewModel
            {
                CartItems = cartItems.Items,
                TotalAmount = cartTotal + shippingFee
            };

            if (User.Identity.IsAuthenticated)
            {
                // Pre-fill shipping info for authenticated users
                var user = await _userService.GetUserByIdAsync(userId);

                if (!user.IsProfileComplete)
                {
                    TempData["RequireProfileCompletion"] = true;
                    return RedirectToAction("Profile", "Account");
                }

                if (string.IsNullOrEmpty(user.Area) || string.IsNullOrEmpty(user.FullName) || string.IsNullOrEmpty(user.StreetAddress) || string.IsNullOrEmpty(user.PhoneNumber))
                {
                    return RedirectToAction("Profile", "Account");
                }

                if (user != null)
                {
                    model.FullName = user.FullName;
                    model.Area = user.Area;
                    model.AddressLine = user.StreetAddress;
                    model.Email = user.Email;
                    model.PhoneNumber = user.PhoneNumber;
                }
            }
            ViewBag.StripePublishableKey = _configuration["Stripe:PublishableKey"];
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).ToList();
                foreach (var error in errors)
                {
                    ModelState.AddModelError("checkout error", error.ErrorMessage);
                }
                var userIid = User.GetUserId(HttpContext);
                var cartItems = await _cartService.GetCartAsync(userIid);
                model.CartItems = cartItems.Items;
                model.TotalAmount = model.CalculatedTotal;
                return View(model);
            }

            var fullAddress = $"{model.Area}, {model.AddressLine}";

            if (!model.PhoneNumber.StartsWith("+974"))
            {
                model.PhoneNumber = $"+974{model.PhoneNumber.TrimStart('0')}";
            }

            var userId = User.GetUserId(HttpContext);
            var cart = await _cartService.GetCartAsync(userId);
            if (!cart.Items.Any()) return RedirectToAction("Index", "Cart");

            try
            {
                // 1. Create Order
                var order = new Order
                {
                    UserId = User.Identity?.IsAuthenticated == true ? userId : null,
                    OrderDate = QatarDateTime.Now,
                    DeliveryDate = model.DeliveryDate.HasValue
                                    ? LocalDateTime.FromDateTime(model.DeliveryDate.Value)
                                        .InZone(QatarDateTime.QatarZone, Resolvers.LenientResolver)
                                        .ToInstant()
                                    : QatarDateTime.Now.Plus(Duration.FromDays(3)),

                    ShippingFee = model.ShippingFee,
                    // Set TotalAmount correctly using CalculatedTotal property
                    TotalAmount = model.CalculatedTotal,
                    Status = OrderStatus.Pending,
                    OrderItems = cart.Items.Select(ci => new OrderItem
                    {
                        SpecialRequest = ci.SpecialRequest,
                        ProductId = ci.ProductId,
                        Quantity = ci.Quantity,
                        Price = ci.Price,
                        Length = ci.Length,
                        Size = ci.Size
                    }).ToList()
                };

                if (!User.Identity?.IsAuthenticated == true)
                {
                    order.GuestFullName = model.FullName;
                    order.GuestAddress = fullAddress;
                    order.GuestPhoneNumber = model.PhoneNumber;
                    order.GuestEmail = model.Email;
                    order.GuestToken = Guid.NewGuid().ToString();
                }

                var createdOrder = await _orderService.CreateOrderAsync(order);
                createdOrder = await _orderService.GetOrderByIdAsync(createdOrder.OrderId);
                await _notificationService.CreateOrderNotification(createdOrder);

                // 2. Create Payment
                var payment = new Payment
                {
                    OrderId = createdOrder.OrderId,
                    UserId = User.Identity?.IsAuthenticated == true ? userId : null,
                    Amount = createdOrder.TotalAmount,
                    PaymentMethod = model.PaymentMethod,
                    Status = PaymentStatus.Pending,
                    PaymentDate = QatarDateTime.Now
                };
                await _paymentService.CreatePaymentAsync(payment, model.PaymentMethod);

                // 3. Link Payment to Order
                createdOrder.PaymentId = payment.Id;
                _context.Orders.Update(createdOrder);
                await _context.SaveChangesAsync();

                // 4. Process Payment (if card)
                if (model.PaymentMethod == "Card")
                {
                    var amountInFils = (int)(decimal.Round(createdOrder.TotalAmount, 2) * 1000);
                    var paymentResponse = await _fatoorahService.CreatePaymentAsync(
                        amountInFils,
                        createdOrder.OrderId.ToString("D8")
                    );
                    payment.PaymentIntentId = paymentResponse.Data.PaymentId;
                    await _paymentService.UpdatePaymentAsync(payment);

                    // Clear cart before redirecting
                    await _cartService.ClearCartAsync(userId);

                    return Redirect(paymentResponse.Data.PaymentURL);
                }

                // 5. For cash, update order status
                createdOrder.Status = OrderStatus.Pending;
                await _orderService.UpdateOrderAsync(createdOrder, new List<long>());
                await _cartService.ClearCartAsync(userId);

                // 6. Send Admin Email   
                var adminEmail = _configuration["EmailSettings:AdminEmail"];
                await SendAdminEmailAsync(adminEmail, createdOrder, payment);

                string customerEmail = User.Identity?.IsAuthenticated == true
                    ? (await _userService.GetUserByIdAsync(userId))?.Email
                    : model.Email;

                if (!string.IsNullOrEmpty(customerEmail))
                {
                    await SendOrderEmailAsync(customerEmail, createdOrder);
                }

                HttpContext.Session.IncrementNotificationCount();

                // Fix: Pass proper parameters to Confirmation action
                return RedirectToAction("Confirmation", new
                {
                    orderId = createdOrder.OrderId,
                    guestToken = User.Identity?.IsAuthenticated != true ? createdOrder.GuestToken : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order for user {UserId}", userId);

                // Restore cart data for the view
                model.CartItems = cart.Items;
                model.TotalAmount = model.CalculatedTotal;
                return View(model);
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> PaymentCallback(string paymentId)
        {
                try
                {
                    var status = await _fatoorahService.GetPaymentStatusAsync(paymentId);
                    if (!status.IsSuccess) return RedirectToAction("CheckoutError");

                    var order = await _orderService.GetOrderByIdAsync(status.OrderId);
                    order.Status = OrderStatus.Processing;
                    await _orderService.UpdateOrderAsync(order, new List<long>());

                    var userId = User.Identity?.IsAuthenticated == true ?
                            User.GetUserId(HttpContext) :
                            HttpContext.Session.GetString("GuestUserId");
             
                    await _cartService.ClearCartAsync(userId);

                    return RedirectToAction("Confirmation", new
                    {
                        orderId = order.OrderId,
                        guestToken = order.GuestToken
                    });
                 }
                catch
                {
                    return RedirectToAction("CheckoutError");
                }
        }

        private async Task<bool> SendAdminEmailAsync(string adminEmail, Order order, Payment payment)
        {
            var subject = $"New Order Notification - #{order.OrderId.ToString("D8")}";
            var templatePath = Path.Combine(_env.WebRootPath, "email-templates", "AdminOrderNotificationEmail.html");

            if (!System.IO.File.Exists(templatePath))
            {
                throw new FileNotFoundException("Admin email template not found.", templatePath);
            }
            // var baseUrl = _configuration["AppSettings:BaseUrl"];
            var dashboardLink = Url.Action("Index", "Dashboard");
            var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";
            var logoUrl = $"{baseUrl}/svgs/avenderline-dark.svg";
            var fullAddress = $"{order.User?.Area}, {order.User?.StreetAddress}";

            string htmlTemplate = await System.IO.File.ReadAllTextAsync(templatePath);
            string message = htmlTemplate
                .Replace("{LogoUrl}", logoUrl)
                .Replace("{OrderId}", order.OrderId.ToString("D8"))
                .Replace("{CustomerName}", order.UserId != null ? order.User?.FullName ?? "Guest" : order.GuestFullName ?? "Guest")
                .Replace("{TotalAmount}", order.TotalAmount.ToString("0.00"))
                .Replace("{Currency}", "QAR")
                .Replace("{PaymentMethod}", payment.PaymentMethod)
                .Replace("{PaymentStatus}", payment.Status.ToString())
                .Replace("{CustomerEmail}", order.UserId != null ? order.User?.Email ?? "N/A" : order.GuestEmail ?? "N/A")
                .Replace("{CustomerPhone}", order.UserId != null ? order.User?.PhoneNumber ?? "N/A" : order.GuestPhoneNumber ?? "N/A")
                .Replace("{CustomerAddress}", order.UserId != null ? fullAddress ?? "N/A" : order.GuestAddress ?? "N/A")
                .Replace("{DashboardLink}", dashboardLink)
                .Replace("{OrderDate}", order.OrderDate.InZone(QatarDateTime.QatarZone).LocalDateTime.ToDateTimeUnspecified().ToString("dd-MMM-yyyy", new CultureInfo("en-QA")));

            try
            {
                await _emailSenderService.SendEmailAsync(adminEmail, subject, message);
                return true;
            }
            catch (Exception)
            {
                // Log the error
                return false;
            }
        }

        private async Task<bool> SendOrderEmailAsync(string email, Order model)
        {
            var subject = "Order Confirmation";
            var templatePath = Path.Combine(_env.WebRootPath, "email-templates", "OrderConfirmationEmail.html");

            if (!System.IO.File.Exists(templatePath))
            {
                // _logger.LogError("Email template not found at: {TemplatePath}", templatePath);
                throw new FileNotFoundException("Email template not found.", templatePath);
            }

            var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";
            var logoUrl = $"{baseUrl}/svgs/avenderline-dark.svg";
            string htmlTemplate = await System.IO.File.ReadAllTextAsync(templatePath);
            string message = htmlTemplate
                 .Replace("{LogoUrl}", logoUrl)
                .Replace("{OrderId}", model.OrderId.ToString("D8"))
                .Replace("{CustomerName}", model.UserId != null ?
                    model.User?.FullName ?? "Customer" :
                    model.GuestFullName ?? "Customer")
                .Replace("{OrderDate}", model.OrderDate
                        .InZone(QatarDateTime.QatarZone)
                        .LocalDateTime
                        .ToString("dd-MMM-yyyy", new CultureInfo("en-QA")))
                .Replace("{TotalAmount}", model.TotalAmount.ToString("N0"))
                .Replace("{Currency}", "QR");

            try
            {
                await _emailSenderService.SendEmailAsync(email, subject, message);
                return true;
            }
            catch (Exception)
            {
                //_logger.LogError(ex, "Failed to send email.");
                return false;
            }

        }

    }
}
