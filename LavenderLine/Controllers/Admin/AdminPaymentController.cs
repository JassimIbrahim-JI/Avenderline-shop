using LavenderLine.Enums.Order;
using LavenderLine.Enums.Payment;
using LavenderLine.VerificationServices;
using LavenderLine.ViewModels.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NodaTime;


namespace LavenderLine.Controllers.Admin
{
    [Authorize(Policy = "RequireAdminRole")]
    public class AdminPaymentController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly EcommerceContext _context;
        private readonly INotificationService _notificationService;
        private readonly IPaymentGatewayService _fatoorahService;
        private readonly ILogger<AdminPaymentController> _logger;

        public AdminPaymentController(IPaymentService paymentService, EcommerceContext context, IPaymentGatewayService stripeService, INotificationService notificationService, ILogger<AdminPaymentController> logger)
        {

            _paymentService = paymentService;
            _context = context;
            _fatoorahService = stripeService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(
          string userId,
          Instant? startDate,
          Instant? endDate,
          PaymentStatus? status = null,
          int page = 1,
          int pageSize = 10,
          string sortBy = "PaymentDate",
          string sortDirection = "desc")
        {
            try
            {
                var (payments, totalCount) = await _paymentService.GetPaginatedPaymentsAsync(
                    userId,
                    startDate,
                    endDate,
                    status,
                    page,
                    pageSize,
                    sortBy,
                    sortDirection);

                return View(new PaymentListViewModel
                {
                    Payments = payments,
                    Pagination = new PaginationModel
                    {
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalItems = totalCount
                    },
                    CurrentSort = sortBy,
                    CurrentDirection = sortDirection,
                    Filters = new PaymentFilterParams
                    {
                        UserId = userId,
                        StartDate = startDate,
                        EndDate = endDate,
                        Status = status
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading payments");
                return RedirectToAction("Index", new { error = "Error loading payments" });
            }
        }

        public async Task<IActionResult> Details(long id)
        {
            var payment = await _paymentService.GetPaymentByIdAsync(id);
            if (payment == null)
            {
                return NotFound();
            }
            return View(payment);
        }

     


        [HttpPost]
        public async Task<IActionResult> ConfirmCashPayment(long paymentId)
        {
            var payment = await _paymentService.GetPaymentByIdAsync(paymentId);
            if (payment == null) return RedirectToAction("Index", new { error = "Payment not found" });

            payment.Status = PaymentStatus.Completed;

            var existingOrder = await _context.Orders.FindAsync(payment.OrderId);
            if (existingOrder != null) existingOrder.Status = OrderStatus.Processing;

            await _context.SaveChangesAsync();
            await _notificationService.SendPaymentStatusChangeNotification(payment, PaymentStatus.Completed);
            return RedirectToAction("Index", new { success = "Cash payment confirmed successfully" });
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmCardPayment(long paymentId)
        {
            var payment = await _paymentService.GetPaymentByIdAsync(paymentId);
            if (payment == null)
            {
                return RedirectToAction("Index", new { error = "Payment not found" });
            }


            if (payment.PaymentMethod != "Card")
            {
                return RedirectToAction("Index", new { error = "Only card payments can be confirmed" });
            }
            if (payment.Status == PaymentStatus.Completed)
            {
                return RedirectToAction("Index", new { error = "Payment is already completed" });
            }
            try
            {
                var paymentIntentId = payment.PaymentIntentId;
                if (string.IsNullOrEmpty(paymentIntentId))
                {
                    return RedirectToAction("Index", new { error = "Payment Intent ID is missing" });
                }

                var paymentIntent = await _fatoorahService.VerifyPaymentAsync(paymentIntentId);
                if (!paymentIntent)
                {
                    return RedirectToAction("Index", new { error = "Payment confirmation failed" });
                }

                payment.Status = PaymentStatus.Completed;

                var order = await _context.Orders.FindAsync(payment.OrderId);
                if (order != null)
                {
                    order.Status = OrderStatus.Processing;
                }

                await _context.SaveChangesAsync();
                await _notificationService.SendPaymentStatusChangeNotification(payment, PaymentStatus.Completed);
                return RedirectToAction("Index", new { success = "Card payment confirmed successfully" });
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", new { error = $"MyFatoorah error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RefundCardPayment(long paymentId, decimal amount)
        {
            try
            {
                var payment = await _paymentService.GetPaymentByIdAsync(paymentId);
                if (payment == null) return Json(new { success = false, message = "Payment not found" });
                var maxRefundable = payment.Amount - payment.RefundedAmount;
                if (amount <= 0 || amount > maxRefundable)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Max refundable: {maxRefundable:N2} QAR"
                    });
                }
                var refundResponse = await _fatoorahService.RefundPaymentAsync(payment.PaymentIntentId,amount);
                if (!refundResponse.IsSuccess)
                {
                    throw new Exception($"Refund failed: {refundResponse.RefundReference}");
                }

                payment.RefundedAmount += amount;
                payment.IsRefunded = payment.RefundedAmount >= payment.Amount;
                payment.RefundDate = QatarDateTime.Now;

                return Json(new
                {
                    success = true,
                    message = "Refund processed successfully",
                    newStatus = "Refunded",
                    newStatusClass = "bg-info",
                    icon = "fa-undo"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refund failed");
                return Json(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> UpdatePaymentStatus(long paymentId, PaymentStatus status)
        {
            var result = await _paymentService.UpdatePaymentStatusAsync(paymentId, status);

            if (result.isValid)
            {
                var payment = await _paymentService.GetPaymentByIdAsync(paymentId);
                if (payment != null && (status == PaymentStatus.Completed || status == PaymentStatus.Refunded))
                {
                    var currentStatus = payment.Status;
                    if (currentStatus == status)
                    {
                        return RedirectToAction("Index", new { error = result.message });
                    }
                    await _notificationService.SendPaymentStatusChangeNotification(payment, status);
                }
            }
            return RedirectToAction("Index", new
            {
                success = result.isValid  ? "Payment status updated" : null,
                error = result.isValid ? null : "Payment not found"
            });
        }
       
    }
}
