using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LavenderLine.Controllers.Admin
{
    [Authorize(Policy = "RequireManagerOrAdminRole")]
    public class AdminPromotionController : Controller
    {
        private readonly IPromotionService _promotionService;

        public AdminPromotionController(IPromotionService promotionService)
        {
            _promotionService = promotionService;
        }


        public async Task<IActionResult> Index()
        {
            var promotions = await _promotionService.GetCurrentPromotionAsync();
            return View(promotions);
        }


        public async Task<IActionResult> Edit(int id)
        {
            var promotion = await _promotionService.GetPromotionByIdAsync(id);
            if (promotion == null)
            {
                return NotFound();
            }
            return View(promotion);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Promotion promotion)
        {
            if (id != promotion.Id)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _promotionService.UpdatePromotionAsync(promotion);
                    return RedirectToAction(nameof(Index));
                }
                catch (ArgumentException ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
                catch (Exception)
                {

                    ModelState.AddModelError(string.Empty, "An error occurred while updating the promotion.");
                }
            }

            return View(promotion);
        }


        public async Task<IActionResult> Delete(int id)
        {
            var promotion = await _promotionService.GetPromotionByIdAsync(id);
            if (promotion == null)
            {
                return NotFound();
            }
            return View(promotion);
        }


        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _promotionService.DeletePromotionAsync(id);
                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An error occurred while deleting the promotion.");
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
