using AutoMapper;
using LavenderLine.ViewModels.Wishlists;
using Microsoft.AspNetCore.Mvc;

namespace LavenderLine.Controllers
{
    public class WishlistController : Controller
    {
        private readonly IWishlistService _wishlistService;
        private readonly IMapper _mapper;

        public WishlistController(IWishlistService wishlistService, IMapper mapper)
        {
            _wishlistService = wishlistService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.GetUserId(HttpContext);
            var wishlist = await _wishlistService.GetWishlistAsync(userId);
            var wishlistCount = wishlist?.Items.Count();
            ViewBag.WishlistCount = wishlistCount;
            return View(_mapper.Map<WishlistViewModel>(wishlist));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromWishlist(int productId, bool returnFull = false)
        {
            var userId = User.GetUserId(HttpContext);
            var result = await _wishlistService.RemoveFromWishlist(productId, userId);
            var wishlistData = await _wishlistService.GetWishlistAsync(userId);
            if (Request.IsAjaxRequest() && !returnFull)
            {

                var itemsHtml = await this.RenderViewAsync("_WishlistPartial", wishlistData.Items, true);

                return Ok(new
                {
                    success = result,
                    itemsHtml,
                    count = wishlistData.Count,
                    items = wishlistData.Items.Select(item => new
                    {
                        productId = item.ProductId,
                        isFavorite = item.IsFavorite
                    })
                });
            }
            var viewModel = _mapper.Map<WishlistViewModel>(wishlistData);
            return RedirectToAction(nameof(Index), viewModel);
        }


        [HttpPost]
        public async Task<IActionResult> ToggleItem(int productId)
        {
            try
            {
                var userId = User.GetUserId(HttpContext);
                var isFavorite = await _wishlistService.ToggleFavoriteAsync(productId, userId);
                var wishlistData = await _wishlistService.GetWishlistAsync(userId);
                var itemsHtml = await this.RenderViewAsync("_WishlistPartial", wishlistData.Items, true);

                return Ok(new
                {
                    success = true,
                    isFavorite,
                    itemsHtml,
                    count = wishlistData.Count,
                    items = wishlistData.Items.Select(item => new
                    {
                        productId = item.ProductId,
                        isFavorite = item.IsFavorite
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCount()
        {
            var userId = User.GetUserId(HttpContext);
            var count = await _wishlistService.GetWishlistCountAsync(userId);
            return Ok(new {  count });
        }
        public async Task<IActionResult> GetWishlistData()
        {
            var userId = User.GetUserId(HttpContext);
            var wishlist = await _wishlistService.GetWishlistAsync(userId);

            if (Request.IsAjaxRequest()) 
            {
                var partialHtml = await this.RenderViewAsync("_WishlistPartial", wishlist.Items, true);

                return Ok(new
                {
                    partialHtml,
                    count = wishlist.Count,
                    items = wishlist.Items.Select(item => new
                    {
                        productId = item.ProductId,
                        isFavorite = item.IsFavorite
                    })
                });
            }

            var viewModel = _mapper.Map<WishlistViewModel>(wishlist);
            return RedirectToAction(nameof(Index), viewModel);

        }


     }

}
