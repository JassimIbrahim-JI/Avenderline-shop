using LavenderLine.Validate;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;


namespace LavenderLine.Controllers
{
    public class NotificationController : Controller
    {

        private readonly EcommerceContext _context;

        public NotificationController(EcommerceContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderNotifications()
        {
            var notifications = await _context.Notifications
                .Where(n => n.Type == NotificationTypes.Order)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToListAsync();

            return Json(notifications.Select(n => new {
                id = n.Id,
                message = n.Message,
                date = n.CreatedAt.InZone(QatarDateTime.QatarZone).LocalDateTime.ToString("g", CultureInfo.CurrentCulture),
                isRead = n.IsRead,
                relatedId = n.RelatedId
            }));
        }

        [HttpGet]
        public async Task<IActionResult> GetLoginMessages()
        {
            var messages = await _context.Notifications
                .Where(n => n.Type == NotificationTypes.Login)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToListAsync();

            return Json(messages.Select(m => new {
                id = m.Id,
                message = m.Message,
                date = m.CreatedAt.InZone(QatarDateTime.QatarZone).LocalDateTime.ToString("g", CultureInfo.CurrentCulture),
                isRead = m.IsRead,
                userId = m.UserId
            }));
        }

        [HttpPost]
        public async Task<IActionResult> MarkNotificationAsRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null && notification.Type == NotificationTypes.Order)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> MarkMessageAsRead(int id)
        {
            var message = await _context.Notifications.FindAsync(id);
            if (message != null && message.Type == NotificationTypes.Login)
            {
                message.IsRead = true;
                await _context.SaveChangesAsync();
            }
            return Json(new { success = true });
        }
    }
}
