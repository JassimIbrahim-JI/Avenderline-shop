using LavenderLine.Storage;
using LavenderLine.ViewModels.Banners;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LavenderLine.Controllers.Admin
{
    [Authorize(Policy = "RequireManagerOrAdminRole")]
    public class CarouselImageController : Controller
    {
        private readonly ICarouselImageService _imageService;
        private readonly IImageStorageService _storageService;
        private readonly ILogger<CarouselImageController> _logger;

        public CarouselImageController(
            ICarouselImageService imageService,
            IImageStorageFactory storageFactory,
            ILogger<CarouselImageController> logger)
        {
            _imageService = imageService;
            _storageService = storageFactory.GetStorageService("Carousel");
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var images = await _imageService.GetAllImagesAsync();
            return View(images);
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CarouselImage model)
        {
            if (!ModelState.IsValid || model.ImageFile == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Invalid data",
                    errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            var result = await _imageService.AddImageAsync(model);
            return Json(new { success = result.isValid, message = result.message });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var image = await _imageService.GetImageByIdAsync(id);
            if (image == null) return NotFound();

            return View(new EditCarouselViewModel
            {
                Id = image.Id,
                ImageUrl = image.ImageUrl,
                Caption = image.Caption,
                Description = image.Description
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditCarouselViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return Json(new
                {
                    success = false,
                    message = "Validation failed",
                    errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            var existingImage = await _imageService.GetImageByIdAsync(vm.Id);
            if (existingImage == null)
                return Json(new { success = false, message = "Image not found." });

            existingImage.Caption = vm.Caption;
            existingImage.Description = vm.Description;
            existingImage.ImageFile = vm.ImageFile; // Assign file if provided

            var result = await _imageService.UpdateImageAsync(existingImage);
            return Json(new { success = result.isValid, message = result.message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _imageService.DeleteImageAsync(id);
            return Json(new { success = result.isValid, message = result.message });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleHome(int id, bool isActiveHome)
        {
            return await ToggleStatus(id, ci => ci.IsActiveHome = isActiveHome);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleShop(int id, bool isActiveShop)
        {
            return await ToggleStatus(id, ci => ci.IsActiveShop = isActiveShop);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleArrivals(int id, bool isActiveArrivals)
        {
            return await ToggleStatus(id, ci => ci.IsActiveArrivals = isActiveArrivals);
        }

        private async Task<IActionResult> ToggleStatus(int id, Action<CarouselImage> setter)
        {
            if (id <= 0) return Json(new { success = false, message = "Invalid request" });

            var image = await _imageService.GetImageByIdAsync(id);
            if (image == null) return Json(new { success = false, message = "Image not found" });

            setter(image);
            var result = await _imageService.UpdateImageAsync(image);

            return Json(new { success = result.isValid, message = result.message });
        }
    }
}
