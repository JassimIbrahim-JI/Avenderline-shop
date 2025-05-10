using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace LavenderLine.Extensions
{
    public static class ControllerExtensions
    {

        public static async Task<string> RenderViewAsync(this Controller controller,
        string viewName, object model, bool partial = false)
        {
            controller.ViewData.Model = model;

            using (var writer = new StringWriter())
            {
                var viewEngine = controller.HttpContext.RequestServices
                    .GetService<ICompositeViewEngine>();

                // Use FindView instead of GetView
                var viewResult = viewEngine.FindView(controller.ControllerContext, viewName, !partial);

                if (!viewResult.Success)
                {
                    throw new InvalidOperationException($"View '{viewName}' not found. Searched locations: " +
                        string.Join(", ", viewResult.SearchedLocations));
                }

                var viewContext = new ViewContext(
                    controller.ControllerContext,
                    viewResult.View,
                    controller.ViewData,
                    controller.TempData,
                    writer,
                    new HtmlHelperOptions()
                );

                await viewResult.View.RenderAsync(viewContext);
                return writer.GetStringBuilder().ToString();
            }
        }
    }

}
