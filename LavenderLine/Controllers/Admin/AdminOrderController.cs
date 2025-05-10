using LavenderLine.Enums.Order;
using LavenderLine.Enums.Payment;
using LavenderLine.ViewModels.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NodaTime;

namespace LavenderLine.Controllers.Admin
{
    [Authorize(Policy = "RequireAdminRole")]
    public class AdminOrderController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IOrderItemService _orderItemService;
        private readonly IUserService _userService;
        private readonly IProductService _productService;
        private readonly ILogger<AdminOrderController> _logger;
        private readonly List<string> _allowedSortColumns = new List<string> { "OrderId", "TotalAmount", "OrderDate" };
        public AdminOrderController(IOrderService orderService,
                                    IOrderItemService orderItemService,
                                    IUserService userService,
                                    IProductService productService,
                                    ILogger<AdminOrderController> logger)
        {
            _orderService = orderService;
            _orderItemService = orderItemService;
            _userService = userService;
            _productService = productService;
            _logger = logger;
        }


        public async Task<IActionResult> Index(
      string userName,
      Instant? startDate,
      OrderStatus? status = null,
      int page = 1,
      int pageSize = 10,
      string sortBy = "OrderDate",
      string sortDirection = "desc")
        {
            try
            {
                var (orders, totalCount) = await _orderService.GetPaginatedOrdersAsync(
                    userName,
                    startDate,
                    status,
                    page,
                    pageSize,
                    sortBy,
                    sortDirection);

                if (!_allowedSortColumns.Contains(sortBy))
                {
                    sortBy = "OrderDate"; // Default to safe column
                }
                var model = new OrderListViewModel
                {
                    Orders = orders,
                    Pagination = new PaginationModel
                    {
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalItems = totalCount
                    },
                    CurrentSort = sortBy,
                    CurrentDirection = sortDirection,
                    FilterParams = new FilterParams
                    {
                        UserName = userName,
                        StartDate = startDate,
                        Status = status
                    }
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading orders");
                return RedirectToAction("Index", new { error = "Error loading orders" });
            }
        }

        public async Task<IActionResult> Details(long id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }

        public async Task<IActionResult> Edit(long id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
          
            if (order == null)
            {
                return NotFound();
            }

            if (order.Payment == null)
            {
                order.Payment = new Payment
                {
                    Status = PaymentStatus.Pending,
                    PaymentMethod = "Cash",
                    PaymentDate = order.OrderDate
                };
            }

            order.OrderItems = await _orderItemService.GetOrderItemsByOrderIdAsync(id);
            order.OrderItems ??= new List<OrderItem>();

            var users = await _userService.GetAllUsersAsync();
            var products = await _productService.GetAllProductsAsync();

            ViewBag.Users = users ?? new List<ApplicationUser>();
            ViewBag.Products = products ?? new List<Product>();

            return View(order);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, Order order, List<long> DeletedItems)
        {
            if (id != order.OrderId) return NotFound();

            // Validate guest information
            if (order.UserId == null &&
                (string.IsNullOrEmpty(order.GuestFullName) ||
                 string.IsNullOrEmpty(order.GuestEmail)))
            {
                ModelState.AddModelError("", "Guest information is required");
                return Json(new { success = false, message = "Guest info required" });
            }

            // Validate order items
            if (order.OrderItems == null || !order.OrderItems.Any())
            {
                ModelState.AddModelError("", "At least one order item is required.");
                return Json(new { success = false, message = "At least one order item is required." });
            }

            foreach (var item in order.OrderItems)
            {
                item.OrderId = order.OrderId;
                if (item.Quantity <= 0 || item.Price <= 0)
                {
                    ModelState.AddModelError("", "Quantity and Price must be greater than 0.");
                    return Json(new { success = false, message = "Invalid item data." });
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    DeletedItems = DeletedItems ?? new List<long>();

                    var result = await _orderService.UpdateOrderAsync(order, DeletedItems);
                    if (result.isValid)
                    {
                        return Json(new { success = true, message = "Order updated successfully" });
                    }
                    return Json(new { success = false, message = result.message });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating order");
                    return Json(new { success = false, message = $"Error: {ex.Message}" });
                }
            }

            var errors = ModelState
          .Where(x => x.Value.Errors.Any())
          .ToDictionary(
              x => x.Key,
              x => x.Value.Errors.Select(e => e.ErrorMessage).ToList()
          );
            _logger.LogError("Validation Errors: {@Errors}", errors);
            return Json(new { success = false, message = "Invalid form data" });
        }

        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var order = await _orderService.GetOrderByIdAsync(id);
                if (order == null)
                {
                    TempData["Error"] = "Order not found";
                    return RedirectToAction(nameof(Index));
                }
                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading delete confirmation");
                TempData["Error"] = "Error loading order details";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            try
            {
                var result = await _orderService.DeleteOrderAsync(id);

                if (!result.isValid)
                {
                    TempData["Error"] = result.message;
                    return RedirectToAction(nameof(Index));
                }

                TempData["Success"] = "Order deleted successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order");
                TempData["Error"] = "Error deleting order";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}