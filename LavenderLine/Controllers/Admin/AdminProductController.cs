using LavenderLine.Storage;
using LavenderLine.ViewModels.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;

namespace LavenderLine.Controllers.Admin
{
    [Authorize(Policy = "RequireManagerOrAdminRole")]
    public class AdminProductController : Controller
    {
        private readonly IProductService _service;
        private readonly ICategoryService _categoryService;
        private readonly IPromotionService _promotionService;
        private readonly IImageStorageService _storageService;
        public AdminProductController(IProductService service, ICategoryService categoryService, IPromotionService promotionService, IImageStorageFactory storageFactory)
        {
            _service = service;
            _categoryService = categoryService;  
            _promotionService = promotionService;
            _storageService = storageFactory.GetStorageService("Product");
        }

        [HttpGet]
        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 8)
        {
            var (products, totalCount) = await _service.GetPagedProductsAsync(pageNumber, pageSize);
            var model = products.Select(p => new ProductViewModel
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Price = p.Price,
                ImageUrl = p.ImageUrl,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name,
                IsActive = p.IsActive,
                IsFeatured = p.IsFeatured,
                IsExcludedFromRelated = p.IsExcludedFromRelated,
                CreatedDate = p.CreatedDate,
                Quantity = p.Quantity,
                Lengths = p.Lengths.Select(c => c.Trim()).ToList(),
                Sizes = p.Sizes.Select(s => s.Trim()).ToList(),
            }).ToList();

            ViewBag.TotalCount = totalCount;
            ViewBag.PageSize = pageSize;
            return View(model);
        }
     
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateCategoriesAsync();
            return View(new ProductViewModel
            {
                Sizes = new List<string>(),
                Lengths = new List<string>(),
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create(ProductViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateCategoriesAsync();
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return Json(new { success = false, message = string.Join("\n", errors) });
            }

            try
            {
                // Upload image and get URL
                string imageUrl = null;
                if (model.ImageFile != null)
                {
                    imageUrl = await _storageService.StoreImageAsync(model.ImageFile);
                    if (string.IsNullOrEmpty(imageUrl))
                    {
                        ModelState.AddModelError("ImageFile", "Invalid image file.");
                        await PopulateCategoriesAsync();
                        return Json(new { success = false, message = "Invalid image file." });
                    }
                }
                else
                {
                    // If ImageFile is required, handle the error
                    ModelState.AddModelError("ImageFile", "Product image is required.");
                    await PopulateCategoriesAsync();
                    return Json(new { success = false, message = "Product image is required." });
                }

                var product = new Product
                {
                    Name = model.Name,
                    Price = model.Price,
                    Description = model.Description,
                    OriginalPrice = model.OriginalPrice,
                    CategoryId = model.CategoryId,
                    IsActive = model.IsActive,
                    IsFeatured = model.IsFeatured,
                    IsExcludedFromRelated = model.IsExcludedFromRelated,
                    CreatedDate = QatarDateTime.Now,
                    ImageUrl = imageUrl,
                    Quantity = model.Quantity,
                    Lengths = model.Lengths.Select(c => c.Trim()).ToList(),
                    Sizes = model.Sizes.Select(s => s.Trim()).ToList(),
                };

                var result = await _service.CreateProductAsync(product, model.ImageFile);
                if (!result.isValid)
                {
                    await PopulateCategoriesAsync();
                    return Json(new { success = false, message = result.message });
                }

                return Json(new { success = true, message = "Product created successfully!" });
            }
            catch (Exception ex)
            {
                await PopulateCategoriesAsync();
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _service.GetProductByIdWithCategoryAsync(id);
            if (product == null) return NotFound();

            var model = new ProductViewModel
            {
                ProductId = product.ProductId,
                Name = product.Name,
                Price = product.Price,
                Description = product.Description,
                OriginalPrice = product.OriginalPrice,
                CategoryId = product.CategoryId,
                ImageUrl = product.ImageUrl,
                IsActive = product.IsActive,
                IsFeatured = product.IsFeatured,
                IsExcludedFromRelated = product.IsExcludedFromRelated,
                Quantity = product.Quantity,
                Lengths = product.Lengths.Select(c => c.Trim()).ToList(),
                Sizes = product.Sizes.Select(s => s.Trim()).ToList(),
                CreatedDate = product.CreatedDate,
            };

            await PopulateCategoriesAsync();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ProductViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateCategoriesAsync();
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return Json(new { success = false, message = string.Join("\n", errors) });
            }

            try
            {
                var product = await _service.GetProductByIdAsync(model.ProductId);
                if (product == null) return Json(new { success = false, message = "Product not found." });

                // Filter out empty strings from lengths and sizes
                model.Lengths = model.Lengths.Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
                model.Sizes = model.Sizes.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

                // Re-validate after filtering
                if (model.Lengths.Count < 2 || model.Sizes.Count < 2)
                {
                    return Json(new
                    {
                        success = false,
                        message = "At least 2 lengths and 2 sizes are required."
                    });
                }


                string oldImageUrl = product.ImageUrl;
                string newImageUrl = null;

                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    newImageUrl = await _storageService.StoreImageAsync(model.ImageFile);
                    product.ImageUrl = newImageUrl;
                }

                // Update main properties
                product.Name = model.Name;
                product.Price = model.Price;
                product.Description = model.Description;
                product.OriginalPrice = model.OriginalPrice;
                product.CategoryId = model.CategoryId;
                product.Quantity = model.Quantity;
                product.Lengths = model.Lengths.Select(c => c.Trim()).ToList();
                product.Sizes = model.Sizes.Select(s => s.Trim()).ToList();

                var result = await _service.UpdateProductAsync(product, model.ImageFile);
                if (!result.isValid)
                {
                    await PopulateCategoriesAsync();
                    return Json(new { success = false, message = result.message });
                }
                if (newImageUrl != null)
                    await _storageService.DeleteImageAsync(oldImageUrl);

                return Json(new { success = true, message = "Product updated successfully!" });
            }
            catch (Exception ex)
            {
                await PopulateCategoriesAsync();
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _service.GetProductByIdWithCategoryAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            var model = new ProductViewModel
            {
                ProductId = product.ProductId,
                Name = product.Name,
                Price = product.Price,
                CategoryId = product.CategoryId,
                CategoryName = product.Category.Name,
                ImageUrl = product.ImageUrl,
                Quantity = product.Quantity,
                CreatedDate = product.CreatedDate,
                IsActive = product.IsActive,
                IsFeatured = product.IsFeatured,
                IsExcludedFromRelated = product.IsExcludedFromRelated,
                Lengths = product.Lengths.Select(c => c.Trim()).ToList(),
                Sizes = product.Sizes.Select(s => s.Trim()).ToList(),
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteProductAsync(id);
            if (!result.isValid)
            {
                Console.WriteLine(result.message);
                return Json(new { success = false, message = "Product not found." });
            }

            return Json(new { success = true, message = "Product Deleted." });
        }


        private async Task PopulateCategoriesAsync()
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            ViewBag.Categories = new SelectList(categories, "CategoryId", "Name");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleIsActive(int id, bool isActive)
        {

            var product = await _service.GetProductByIdAsync(id);
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found." });
            }

            product.IsActive = isActive;

           var result = await _service.UpdateProductAsync(product);
            if (!result.isValid)
                return Json(new { success = false, result.message }); 

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleIsFeatured(int id, bool isFeatured)
        {

            var product = await _service.GetProductByIdAsync(id);
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found." });
            }

            product.IsFeatured = isFeatured;
            var result = await _service.UpdateProductAsync(product);
            if (!result.isValid)
                return Json(new { success = false, message = result.message });

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleIsExcludedFromRelated(int id, bool isExcludedFromRelated)
        {

            var product = await _service.GetProductByIdAsync(id);
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found." });
            }

            product.IsExcludedFromRelated = isExcludedFromRelated;
           var result = await _service.UpdateProductAsync(product);
            if (!result.isValid)
                return Json(new { success = false,  result.message });

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPromotion(Promotion promotion)
        {
            if (promotion.ProductId <= 0)
            {
                return Json(new { success = false, message = "Product ID is required." });
            }

            if (string.IsNullOrEmpty(promotion.Category))
            {
                return Json(new { success = false, message = "Category is required." });
            }

            try
            {
                var product = await _service.GetProductByIdWithCategoryAsync(promotion.ProductId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found." });
                }

                // Populate Product in Promotion
                promotion.Product = product;

                // Create the promotion
                await _promotionService.CreatePromotionAsync(promotion);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemovePromotion(int productId)
        {
            try
            {
                // Remove the promotion from the database
                await _promotionService.DeletePromotionByProductIdAsync(productId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCurrentPromotion(int productId)
        {
            var promotion = await _promotionService.GetCurrentPromotionByProductIdAsync(productId);
            return Json(new { endDate = promotion.EndDate.InZone(QatarDateTime.QatarZone).LocalDateTime.ToString("o",CultureInfo.CurrentCulture)});
        }

    }
}
