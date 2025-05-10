using LavenderLine.Enums.Order;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using LavenderLine.Enums.Payment;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using System.Globalization;
using NodaTime;

namespace LavenderLine.Controllers
{
    public class PdfController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PdfController> _logger;

        // Font configuration
        private static readonly PdfFont _headerFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
        private static readonly PdfFont _bodyFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
        private static readonly Color _darkGray = new DeviceRgb(64, 64, 64);
        private static readonly Color _gray = new DeviceRgb(128, 128, 128);


        public PdfController(IOrderService orderService, ILogger<PdfController> logger, IPaymentService paymentService)
        {
            _orderService = orderService;
            _logger = logger;
            _paymentService = paymentService;
        }


        [HttpGet]
        public async Task<IActionResult> ExportToPdf(
                   string userName,
                   DateTime? startDate,
                   OrderStatus? status = null)
        {
            // Convert incoming DateTime? to NodaTime Instant?
            var startInstant = ConvertToInstant(startDate);

            var orders = await _orderService.GetOrdersForExportAsync(userName, startInstant, status);
            return GeneratePdfFile(orders);
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmExportPdf()
        {
            try
            {
                if (TempData["ExportParams"] == null)
                    return RedirectToAction("Index", new { error = "Export parameters not found" });

                dynamic p = JsonConvert.DeserializeObject<dynamic>(TempData["ExportParams"].ToString());
                var userName = (string)p.userName;
                DateTime? sd = ParseDate(p.startDate);
                var status = ParseStatus(p.status);
                var startInstant = ConvertToInstant(sd);

                var orders = await _orderService.GetOrdersForExportAsync(userName, startInstant, status);
                return GeneratePdfFile(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Confirmed export failed");
                return RedirectToAction("Index", new { error = "Error during confirmed export" });
            }
        }

        // Helper: convert DateTime? to Instant? in Qatar zone
        private Instant? ConvertToInstant(DateTime? date)
        {
            if (!date.HasValue) return null;
            var ld = LocalDate.FromDateTime(date.Value);
            var zoned = QatarDateTime.QatarZone.AtStrictly(ld.AtMidnight());
            return zoned.ToInstant();
        }

        private FileResult GeneratePdfFile(IEnumerable<Order> orders)
        {
            using var stream = new MemoryStream();
            using var writer = new PdfWriter(stream);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf, PageSize.A4.Rotate());

            document.SetMargins(20, 20, 20, 20);

            document.Add(new Paragraph("Order Report - Lavender Line")
                .SetFont(_headerFont)
                .SetFontSize(18)
                .SetFontColor(_darkGray)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(20));

            var table = new Table(new[] { 1.5f, 2f, 2f, 2f, 2f, 2f, 3f })
                .SetWidth(UnitValue.CreatePercentValue(100));

            // Headers
            AddTableHeader(table, "Order ID", _darkGray);
            AddTableHeader(table, "Customer", _gray);
            AddTableHeader(table, "Email", _gray);
            AddTableHeader(table, "Phone", _gray);
            AddTableHeader(table, "Total (QAR)", _gray);
            AddTableHeader(table, "Status", _gray);
            AddTableHeader(table, "Items", _gray);

            // Rows
            foreach (var order in orders)
            {
                AddTableCell(table, $"#{order.OrderId:D8}");
                AddTableCell(table, order.User?.FullName ?? order.GuestFullName);
                AddTableCell(table, order.User?.Email ?? order.GuestEmail);
                AddTableCell(table, order.User?.PhoneNumber ?? order.GuestPhoneNumber);
                AddTableCell(table, order.TotalAmount.ToString("N2"));

                // Safe date formatting
                var formatted = order.OrderDate
                    .InZone(QatarDateTime.QatarZone)
                    .LocalDateTime
                    .ToDateTimeUnspecified()
                    .ToString("dd MMM yyyy HH:mm", new CultureInfo("en-QA"));
                AddTableCell(table, formatted);

                // Status cell
                var statusCell = new Cell()
                    .Add(new Paragraph(order.Status.ToString()))
                    .SetFontColor(GetStatusColor(order.Status));
                table.AddCell(statusCell);

                // Items list
                var items = order.OrderItems.Any()
                    ? string.Join("\n", order.OrderItems.Select(i =>
                        $"{i.Product?.Name} ({i.Quantity}x {i.Price:N2} QAR)"))
                    : "No items";
                AddTableCell(table, items);
            }

            document.Add(table);

            return File(stream.ToArray(), "application/pdf",
                $"Orders_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        }

        #region Helpers
        private void AddTableHeader(Table table, string text, Color backgroundColor)
        {
            table.AddHeaderCell(new Cell()
                .SetBackgroundColor(backgroundColor)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetPadding(5)
                .Add(new Paragraph(text)
                    .SetFont(_headerFont)
                    .SetFontColor(DeviceRgb.WHITE)));
        }

        private void AddTableCell(Table table, string content)
        {
            table.AddCell(new Cell()
                .Add(new Paragraph(content))
                .SetFont(_bodyFont)
                .SetPadding(5));
        }

        private Color GetStatusColor(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Delivered => new DeviceRgb(0, 128, 0),    // Green
                OrderStatus.Cancelled => new DeviceRgb(255, 0, 0),    // Red
                OrderStatus.Processing => new DeviceRgb(0, 0, 255),   // Blue
                _ => DeviceRgb.BLACK
            };
        }

        private Color GetPaymentStatusColor(PaymentStatus status)
        {
            return status switch
            {
                PaymentStatus.Completed => new DeviceRgb(0, 128, 0),  // Green
                PaymentStatus.Refunded => new DeviceRgb(0, 0, 255),   // Blue
                PaymentStatus.Failed => new DeviceRgb(255, 0, 0),     // Red
                _ => DeviceRgb.BLACK
            };
        }
        #endregion


        [HttpGet]
        public async Task<IActionResult> ExportToPdfForPayments(
      string userId,
      DateTime? startDate,
      DateTime? endDate,
      PaymentStatus? status = null)
        {
            // convert incoming DateTime? → Instant? in Qatar zone
            Instant? startInstant = null, endInstant = null;
            if (startDate.HasValue)
            {
                var ld = LocalDate.FromDateTime(startDate.Value);
                startInstant = QatarDateTime.QatarZone
                                 .AtStrictly(ld.AtMidnight())
                                 .ToInstant();
            }
            if (endDate.HasValue)
            {
                // treat endDate as end of day
                var ld = LocalDate.FromDateTime(endDate.Value);
                endInstant = QatarDateTime.QatarZone
                                 .AtStrictly(ld.PlusDays(1).AtMidnight())
                                 .ToInstant();
            }

            var count = await _paymentService.GetPaymentCountAsync(userId, startInstant, status);
            if (count > 500)
            {
                TempData["ExportConfirmation"] = true;
                TempData["ExportParams"] = JsonConvert.SerializeObject(new
                {
                    userId,
                    startDate,
                    endDate,
                    status
                });
                return RedirectToAction("Index", "AdminPayment", new
                {
                    warning = $"This export will include {count} records. Confirm to proceed."
                });
            }

            var payments = await _paymentService.GetPaymentForExportAsync(userId, startInstant, status);
            return  GeneratePaymentsPdfFile(payments);
        }

        public async Task<IActionResult> ConfirmExportPdfForPayments()
        {
            if (TempData["ExportParams"] == null)
                return RedirectToAction("Index", "AdminPayment", new { error = "Export parameters not found" });

            dynamic p = JsonConvert.DeserializeObject<dynamic>(TempData["ExportParams"].ToString());
            DateTime? sd = ParseDate(p.startDate), ed = ParseDate(p.endDate);
            var status = ParseStatusPayment(p.status);

            // same conversion logic as above
            Instant? startInstant = sd.HasValue
              ? QatarDateTime.QatarZone.AtStrictly(LocalDate.FromDateTime(sd.Value).AtMidnight()).ToInstant()
              : null;
            Instant? endInstant = ed.HasValue
              ? QatarDateTime.QatarZone.AtStrictly(LocalDate.FromDateTime(ed.Value).PlusDays(1).AtMidnight()).ToInstant()
              : null;

            var payments = await _paymentService.GetPaymentForExportAsync(p.userId.ToString(), startInstant, status);
            return GeneratePaymentsPdfFile(payments);
        }

        private FileResult GeneratePaymentsPdfFile(IEnumerable<Payment> payments)
        {
            using var stream = new MemoryStream();
            using var writer = new PdfWriter(stream);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf, PageSize.A4.Rotate());

            document.SetMargins(20, 20, 20, 20);

            document.Add(new Paragraph("Payment Report - Lavender Line")
                .SetFont(_headerFont)
                .SetFontSize(18)
                .SetFontColor(_darkGray)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(20));

            var table = new Table(new[] { 1.5f, 2f, 2f, 1.5f, 1.5f, 3f })
                .SetWidth(UnitValue.CreatePercentValue(100));

            AddTableHeader(table, "Payment ID", _darkGray);
            AddTableHeader(table, "Date", _gray);
            AddTableHeader(table, "User", _gray);
            AddTableHeader(table, "Amount (QAR)", _gray);
            AddTableHeader(table, "Status", _gray);
            AddTableHeader(table, "Method", _gray);

            foreach (var p in payments)
            {
                AddTableCell(table, $"#{p.Id:D8}");

                // ← convert LocalDateTime → DateTime, then format
                var formattedDate = p.PaymentDate
                    .InZone(QatarDateTime.QatarZone)
                    .LocalDateTime
                    .ToDateTimeUnspecified()                                  // ← key step
                    .ToString("dd MMM yyyy HH:mm", new CultureInfo("en‑QA"));

                AddTableCell(table, formattedDate);
                AddTableCell(table, p.User?.FullName ?? p.Order.GuestFullName);
                AddTableCell(table, p.Amount.ToString("N2"));

                var statusCell = new Cell()
                    .Add(new Paragraph(p.Status.ToString()))
                    .SetFontColor(GetPaymentStatusColor(p.Status));
                table.AddCell(statusCell);

                AddTableCell(table, p.PaymentMethod);
            }

            document.Add(table);

            // flush & return
            return File(stream.ToArray(),
                        "application/pdf",
                        $"Payments_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        }


        private DateTime? ParseDate(object o)
          => DateTime.TryParse(o?.ToString(), out var dt) ? dt : (DateTime?)null;
        private PaymentStatus? ParseStatusPayment(object o)
            => Enum.TryParse<PaymentStatus>(o?.ToString(), out var st) ? st : (PaymentStatus?)null;
        private OrderStatus? ParseStatus(object o)
            => Enum.TryParse<OrderStatus>(o?.ToString(), out var st) ? st : (OrderStatus?)null;
    }
}
