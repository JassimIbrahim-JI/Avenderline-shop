using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LavenderLine.Controllers.Admin
{
    [Authorize(Policy = "RequireManagerOrAdminRole")]
    public class AdminDashboardController : Controller
    {
        private readonly AnalyticsService _analyticsService;

        public AdminDashboardController(AnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }


        public async Task<IActionResult> Index()
        {
            ViewData["ActiveLink"] = "Dashboard";
            var dashboardData = await _analyticsService.GetDashboardDataAsync();
            if (dashboardData == null)
            {
                return NotFound();
            }

            return View(dashboardData);
        }
    }
}
