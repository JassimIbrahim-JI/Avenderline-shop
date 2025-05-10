using LavenderLine.Models;
using LavenderLine.Storage;
using LavenderLine.Validate;
using Microsoft.EntityFrameworkCore;
using static System.Net.Mime.MediaTypeNames;

namespace LavenderLine.Services
{
    public interface IInstagramPostService
    {
        Task<IEnumerable<InstagramPost>> GetAllPostsAsync();
        Task<InstagramPost> GetPostByIdAsync(int id);
        Task<ValidateResult> AddPostAsync(InstagramPost post);
        Task<ValidateResult> DeletePostAsync(int id);
    }
    public class InstagramPostService : IInstagramPostService
    {

        private readonly EcommerceContext _context;
        private readonly IImageStorageService _storageService;
        public InstagramPostService(EcommerceContext context, IImageStorageFactory storageFactory)
        {
            _context = context;
            _storageService = storageFactory.GetStorageService("Post");
        }

        public async Task<IEnumerable<InstagramPost>> GetAllPostsAsync()
        {
            return await _context.InstagramPosts.ToListAsync();
        }

        public async Task<InstagramPost> GetPostByIdAsync(int id)
        {
            return await _context.InstagramPosts.FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<ValidateResult> AddPostAsync(InstagramPost post)
        {
            if (post.ImageFile == null)
                return new ValidateResult(false, "Image file is required.");
            try
            {
                post.ImageUrl = await _storageService.StoreImageAsync(post.ImageFile);
                await _context.InstagramPosts.AddAsync(post);
                await _context.SaveChangesAsync();

                return new ValidateResult(true, "Post added successfully.");
            }
            catch (Exception ex)
            {
                // Cleanup uploaded image if DB fails
                if (!string.IsNullOrEmpty(post.ImageUrl))
                    await _storageService.DeleteImageAsync(post.ImageUrl);

                return new ValidateResult(false, $"Failed to add post: {ex.Message}");
            }
        }

        public async Task<ValidateResult> DeletePostAsync(int id)
        {
                var post = await _context.InstagramPosts.FindAsync(id);
                if (post == null)
                    return new ValidateResult(false, "Post not found.");

                string imageUrl = post.ImageUrl;
                _context.InstagramPosts.Remove(post);
                await _context.SaveChangesAsync();

                // Delete image ONLY after DB succeeds
                if (!string.IsNullOrEmpty(imageUrl))
                    await _storageService.DeleteImageAsync(imageUrl);
                return new ValidateResult(true, "Post deleted successfully.");
            }
    }
}
