using AutoMapper;
using LavenderLine.Exceptions;
using LavenderLine.ViewModels.Carts;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace LavenderLine.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private readonly IProductService _productService;
        private readonly IMapper _mapper;

        public CartController(ICartService cartService, IMapper mapper, IProductService productService)
        {
            _cartService = cartService;
            _mapper = mapper;
            _productService = productService;
        }

        public async Task<IActionResult> Index()
        {
            HttpContext.Session.TryGetValue("__dummy", out _);
            var userId = User.GetUserId(HttpContext);
            var cartDto = await _cartService.GetCartAsync(userId);
            var cartViewModel = _mapper.Map<CartViewModel>(cartDto);
            cartViewModel.Items = _mapper.Map<List<CartItemViewModel>>(cartDto.Items) ?? new List<CartItemViewModel>();
            return View(cartViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddItem(AddToCartRequest request)
        {
            try
            {

                var userId = User.GetUserId(HttpContext);
                await _cartService.AddToCartAsync(userId, request);
                var cartData = await _cartService.GetCartAsync(userId);

                return Json(new
                {
                    success = true,
                    message = "Item added to cart",
                    itemsHtml = await this.RenderViewAsync("_CartItemsPartial", cartData.Items, true),
                    total = cartData.Total,
                    count = cartData.Count
                });

            }
            catch (InsufficientStockException ex)
            {
                return Json(new { error = ex.Message, showSoldOut = true });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromCart(RemoveCartItemRequest request, bool returnFull = false)
        {
            try
            {

                var userId = User.GetUserId(HttpContext);
                await _cartService.RemoveFromCartAsync(userId, request);
                var cartData = await _cartService.GetCartAsync(userId);

                if (Request.IsAjaxRequest() && !returnFull)
                {

                    return Json(new
                    {
                        success = true,
                        itemsHtml = await this.RenderViewAsync("_CartItemsPartial", cartData.Items, true),
                        total = cartData.Total.ToString("C2", new CultureInfo("en-QA")),
                        count = cartData.Count,

                    });
                }
                else
                {

                    var viewModel = _mapper.Map<CartViewModel>(cartData);
                    return View("Index", viewModel);
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetCartSummary()
        {
            var userId = User.GetUserId(HttpContext);
            var cartData = await _cartService.GetCartAsync(userId);

            if (Request.IsAjaxRequest())
            {

                return Json(new
                {
                    items = cartData.Items,
                    itemsHtml = await this.RenderViewAsync("_CartItemsPartial", cartData.Items, true),
                    total = cartData.Total,
                    count = cartData.Count
                });
            }
            else
            {

                var viewModel = _mapper.Map<CartViewModel>(cartData);
                return View("Index", viewModel);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                var userId = User.GetUserId(HttpContext);

                await _cartService.ClearCartAsync(userId);
                var cartData = await _cartService.GetCartAsync(userId);

                return Json(new
                {
                    success = true,
                    message = "Cart cleared successfully",
                    itemsHtml = await this.RenderViewAsync("_CartItemsPartial", cartData.Items, true),
                    total = cartData.Total.ToString("N0"),
                    count = cartData.Count
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }


        [HttpGet]
        [ResponseCache(Duration = 10)]
        public async Task<IActionResult> GetProductStock(int productId)
        {
            if (productId <= 0)
            {
                return BadRequest(new { success = false, error = "Invalid product ID." });
            }

            try
            {
                var product = await _productService.GetProductByIdAsync(productId);
                if (product == null)
                {
                    return NotFound(new { success = false, error = "Product not found." });
                }

                return Ok(new
                {
                    success = true,
                    quantity = product.Quantity,
                    lastUpdated = QatarDateTime.Now,
                    lengths = product.Lengths, 
                    sizes = product.Sizes, 
                    selectedLength = product.Lengths.FirstOrDefault(), 
                    selectedSize = product.Sizes.FirstOrDefault() 
                });
            }
            catch (Exception)
            {
         
                return StatusCode(500, new
                {
                    success = false,
                    error = "An error occurred while fetching product stock."
                });
            }
        }

    }
}


