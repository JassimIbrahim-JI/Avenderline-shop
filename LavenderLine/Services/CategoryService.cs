using LavenderLine.Storage;
using LavenderLine.Validate;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace LavenderLine.Services
{

    public interface ICategoryService
    {
        Task<IEnumerable<Category>> GetAllCategoriesAsync(params Expression<Func<Category, object>>[] includes);
        Task<ValidateResult> CreateCategoryAsync(Category category, IFormFile? imageFile);
        Task<ValidateResult> UpdateCategoryAsync(Category category, IFormFile? imageFile);
        Task<ValidateResult> DeleteCategoryAsync(int id);
        Task<Category?> GetCategoryByIdAsync(int id);
        Task<Category?> GetCategoryByNameAsync(string categoryName);
        Task<IEnumerable<Category>> GetAllCategoriesAsync();
        Task<List<Category>> GetBannerCategoriesAsync(params Expression<Func<Category, object>>[] includes);
        Task<(IEnumerable<Category> Items, int TotalCount)> GetPagedCategoriesAsync(int pageNumber, int pageSize);
        Task<IEnumerable<Category>> SearchCategoriesAsync(string searchTerm);
        Task<ValidateResult> UpdateBannerStatusAsync(int categoryId, bool isBanner);
    }

    public class CategoryService : ICategoryService
    {

        private readonly EcommerceContext _context;
        private readonly IImageStorageService _storageService;
        public CategoryService(EcommerceContext context, IImageStorageFactory storageFactory)
        {
            _context = context;
            _storageService = storageFactory.GetStorageService("Category");
        }

        public async Task<IEnumerable<Category>> GetAllCategoriesAsync(params Expression<Func<Category, object>>[] includes)
        {
            IQueryable<Category> query = _context.Categories;
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
            return await query.ToListAsync();
        }

        public async Task<ValidateResult> CreateCategoryAsync(Category category, IFormFile? imageFile)
        {
            // Validate category (image is optional)
            var validationResults = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(category, new ValidationContext(category), validationResults, true);

            if (!isValid)
            {
                return new ValidateResult(false, string.Join(", ", validationResults.Select(v => v.ErrorMessage)));
            }

            try
            {
                if (imageFile != null)
                {
                    category.ImageUrl = await _storageService.StoreImageAsync(imageFile);
                }

                await _context.Categories.AddAsync(category);
                await _context.SaveChangesAsync();
                return new ValidateResult(true, "Category created successfully.");
            }
            catch
            {
                if (!string.IsNullOrEmpty(category.ImageUrl))
                    await _storageService.DeleteImageAsync(category.ImageUrl);

                throw;
            }
        }


        public async Task<ValidateResult> UpdateCategoryAsync(Category category, IFormFile? imageFile)
        {
            var existingCategory = await _context.Categories
               .FirstOrDefaultAsync(c => c.CategoryId == category.CategoryId);

            if (existingCategory == null)
                return new ValidateResult(false, "Category not found.");

            string oldImageUrl = existingCategory.ImageUrl;
            string newImageUrl = null;

            try
            {
                // Update image only if new file provided
                if (imageFile != null)
                {
                    newImageUrl = await _storageService.StoreImageAsync(imageFile);
                    existingCategory.ImageUrl = newImageUrl;
                }

                existingCategory.Name = category.Name;
                existingCategory.IsBanner = category.IsBanner;

                _context.Categories.Update(existingCategory);
                await _context.SaveChangesAsync();

                // Delete old image only after successful update
                if (imageFile != null && !string.IsNullOrEmpty(oldImageUrl))
                {
                    await _storageService.DeleteImageAsync(oldImageUrl);
                }

                return new ValidateResult(true, "Category updated successfully.");
            }
            catch
            {
                // Rollback new image
                if (newImageUrl != null)
                    await _storageService.DeleteImageAsync(newImageUrl);

                throw;
            }
        }

        public async Task<ValidateResult> DeleteCategoryAsync(int id)
        {
            if (id <= 0)
                return new ValidateResult(false, "Invalid category ID.");

            var executionStrategy = _context.Database.CreateExecutionStrategy();
            return await executionStrategy.ExecuteAsync(async () =>
            {

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var category = await _context.Categories.FindAsync(id);
                    if (category == null)
                        return new ValidateResult(false, "Category not found.");

                    if (await _context.Products.AnyAsync(p => p.CategoryId == id))
                        return new ValidateResult(false, "Cannot delete category with associated products.");

                    string imageUrl = category.ImageUrl;

                    _context.Categories.Remove(category);
                    await _context.SaveChangesAsync();

                    if (!string.IsNullOrEmpty(imageUrl))
                        await _storageService.DeleteImageAsync(imageUrl);

                    await transaction.CommitAsync();
                    return new ValidateResult(true, "Category deleted successfully.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return new ValidateResult(false, $"Delete failed: {ex.Message}");
                }

            });          
        }

        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            return await _context.Categories.Include(c=>c.Products)
                                 .FirstOrDefaultAsync(c => c.CategoryId == id);
        }

        public async Task<Category?> GetCategoryByNameAsync(string categoryName)
        {

            return await _context.Categories
                                 .FirstOrDefaultAsync(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {
            return await _context.Categories.ToListAsync();
        }

        public async Task<List<Category>> GetBannerCategoriesAsync(params Expression<Func<Category, object>>[] includes)
        {
            IQueryable<Category> query = _context.Categories.Where(c => c.IsBanner); 

            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            return await query.ToListAsync(); 
        }



        public async Task<(IEnumerable<Category> Items, int TotalCount)> GetPagedCategoriesAsync(int pageNumber, int pageSize)
        {
            pageNumber = pageNumber < 1 ? 1 : pageNumber;

            var totalCount = await _context.Categories.CountAsync();
            var items = await _context.Categories.Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (items, totalCount);

        }
        private List<string> ValidateCategory(Category category)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(category.Name))
            {
                errors.Add("Name is required.");
            }
            return errors;
        }

        public async Task<IEnumerable<Category>> SearchCategoriesAsync(string searchTerm)
        {
            return await _context.Categories
                .Where(c => c.Name.Contains(searchTerm))
                .ToListAsync();
        }

        public async Task<ValidateResult> UpdateBannerStatusAsync(int categoryId, bool isBanner)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == categoryId);

            if (category == null)
            {
                return new ValidateResult(false, "Category not found.");
            }

            try
            {
                category.IsBanner = isBanner;
                _context.Categories.Update(category);
                await _context.SaveChangesAsync();
                return new ValidateResult(true, "Banner status updated successfully.");
            }
            catch (Exception ex)
            {
                return new ValidateResult(false, $"Failed to update banner status: {ex.Message}");
            }
        }
    }
}
