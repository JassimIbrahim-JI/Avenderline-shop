using LavenderLine.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LavenderLine.Controllers.Admin
{
    [Authorize(Policy = "RequireManagerOrAdminRole")]
    public class AdminInstagramPostController : Controller
    {
        private readonly IInstagramPostService _instagramPostService;
        private readonly IImageStorageService _storageService;

        public AdminInstagramPostController(IInstagramPostService instagramPostService, IImageStorageFactory storageFactory)
        {
            _instagramPostService = instagramPostService;
            _storageService = storageFactory.GetStorageService("Post");
        }


        public async Task<IActionResult> Index()
        {
            var posts = await _instagramPostService.GetAllPostsAsync();
            return View(posts);
        }


        public IActionResult Create() => View();


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InstagramPost post)
        {
            if (!ModelState.IsValid || post.ImageFile == null)
            {
                return Json(new
                {
                    success = false,
                    errors = ModelState.Values
                        .SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            var result = await _instagramPostService.AddPostAsync(post);
            return Json(new { success = result.isValid, message = result.message });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _instagramPostService.DeletePostAsync(id);
            return Json(new { success = result.isValid, message = result.message });
        }


    }
}
