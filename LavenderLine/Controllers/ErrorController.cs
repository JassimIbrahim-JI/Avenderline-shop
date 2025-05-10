using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LavenderLine.Controllers
{
    public class ErrorController : Controller
    {

        private readonly ILogger<ErrorController> _logger;
        public ErrorController(ILogger<ErrorController> logger)
        {
            _logger = logger;
        }

        [Route("/Error")]
        public IActionResult HandleError()
        {
            var exceptionHandlerPathFeature =
                HttpContext.Features.Get<IExceptionHandlerPathFeature>();

            _logger.LogError(exceptionHandlerPathFeature.Error, "Global error occurred");
            return View("Error");
        }
    }
}
