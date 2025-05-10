using LavenderLine.Storage;
using LavenderLine.ViewModels.Banners;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace LavenderLine.Controllers.Admin
{
    [Authorize(Policy = "RequireManagerOrAdminRole")]
    public class AdminCategoryController : Controller
    {
        private readonly ICategoryService _categoryService;
        private readonly IImageStorageService _storageService;
        public AdminCategoryController(ICategoryService categoryService, IImageStorageFactory storageFactory)
        {
            _categoryService = categoryService;
            _storageService = storageFactory.GetStorageService("Category");
        }

        
        [HttpGet]
        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10)
        {
            var (categories, totalCount) = await _categoryService.GetPagedCategoriesAsync(pageNumber, pageSize);
            ViewBag.TotalCount = totalCount;
            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            var model = categories.Select(c => new CategoryViewModel
            {
                CategoryId = c.CategoryId,
                Name = c.Name,
                IsBanner = c.IsBanner,
                ImageUrl = c.ImageUrl,
            }).ToList();

            return View(model);
        }

     
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null)
            {
                return Json(new { success = false, message = "Category not found." });
            }

            return View(category);
        }

        
        [HttpGet]
        public IActionResult Create()
        {
            var model = new CategoryViewModel(); 
            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
              .Where(e => e.Value.Errors.Count > 0)
              .ToDictionary(
                  kvp => kvp.Key,
                  kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
              );
                return Json(new { success = false, message = "Validation failed", errors });
            }

            try
            {
             
              var category = new Category
                {
                    Name = model.Name.Trim(),
                    IsBanner = model.IsBanner
                };

                var result = await _categoryService.CreateCategoryAsync(category, model.ImageFile);
                return Json(new
                {
                    success = result.isValid,
                    message = result.isValid ? "Category created!" : result.message
                });
            }
            catch (ArgumentException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An unexpected error occurred." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null)
            {
                return Json(new { success = false, message = "Category not found." });
            }

            var model = new CategoryViewModel
            {
                CategoryId = category.CategoryId,
                Name = category.Name,
                ImageUrl = category.ImageUrl,
                IsBanner = category.IsBanner,
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(CategoryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(e => e.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                return Json(new { success = false, errors });
            }

            try
            {
                var category = new Category
                {
                    CategoryId = model.CategoryId,
                    Name = model.Name.Trim(),
                    IsBanner = model.IsBanner
                };

                var result = await _categoryService.UpdateCategoryAsync(category, model.ImageFile);

                return Json(new
                {
                    success = result.isValid,
                    message = result.isValid ? "Category updated!" : result.message,
                    redirectUrl = Url.Action("Index")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _categoryService.DeleteCategoryAsync(id);

                if (!result.isValid)
                {
                    return Json(new { success = false, message = result.message });
                }

                return Json(new
                {
                    success = true,
                    message = "Category deleted successfully!",
                    deletedId = id
                });
            }
            catch (Exception)
            {
                return Json(new
                {
                    success = false,
                    message = "A system error occurred. Please try again later."
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleBanner(int id, bool isBanner)
        {
            try
            {
                var category = await _categoryService.GetCategoryByIdAsync(id);
                if (category == null)
                {
                    return Json(new { success = false, message = "Category not found" });
                }

                var result = await _categoryService.UpdateBannerStatusAsync(id, isBanner);

                return Json(new
                {
                    success = result.isValid,
                    message = result.isValid ? "Banner status updated!" : result.message
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Error updating banner status: {ex.Message}"
                });
            }
        }


    }
}