using LavenderLine.Storage;
using LavenderLine.Validate;
using LavenderLine.ViewModels.Products;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace LavenderLine.Services
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetAllProductsAsync(bool useSplitQuery = false, params Expression<Func<Product, object>>[] includes);
        Task<IEnumerable<ProductViewModel>> GetActiveProducts();
        Task<ValidateResult> DeleteProductAsync(int id);
        Task<Product> GetProductByIdAsync(int id);
        Task<Product> GetProductByIdWithCategoryAsync(int id);
        Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId);
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<List<ProductViewModel>> GetRelatedProductsAsync(string? categoryName = null, int currentProductId = 0, int maxItems = 4, bool showExcludedOnly = false);
        Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedProductsAsync(int pageNumber, int pageSize);
        Task<ValidateResult> CreateProductAsync(Product product, IFormFile? imageFile);
        Task<ValidateResult> UpdateProductAsync(Product product, IFormFile? imageFile = null);
        Task<List<ProductViewModel>> GetFeaturedProductAsync();
        Task<(IEnumerable<ProductViewModel> Products, int TotalCount)> GetFilteredProductsAsync(string category, decimal minPrice, decimal maxPrice, string sortBy, int page, int pageSize,bool showExcludedOnly = false);
        Task<(decimal MinPrice, decimal MaxPrice)> GetPriceRangeAsync(string category = null);
        Task<IEnumerable<ProductViewModel>> SearchProductsAsync(string searchTerm);
    }
    public class ProductService : IProductService
    {

        private readonly EcommerceContext _context;
        private readonly IImageStorageService _storageService;
        public ProductService(EcommerceContext context, IImageStorageFactory storageFactory)
        {
            _context = context;
            _storageService = storageFactory.GetStorageService("Product");
        }
        public async Task<IEnumerable<Product>> GetAllProductsAsync(bool useSplitQuery = false, params Expression<Func<Product, object>>[] includes)
        {
            IQueryable<Product> query = _context.Products;
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
            if (useSplitQuery)
            {
                query = query.AsSplitQuery();
            }

            return await query.ToListAsync();
        }
        public async Task<ValidateResult> DeleteProductAsync(int id)
        {
                var product = await _context.Products
                    .Include(p => p.OrderItems)
                    .FirstOrDefaultAsync(p => p.ProductId == id);

                if (product == null)
                    return new ValidateResult(false, "Product not found.");

                if (product.OrderItems.Any())
                    return new ValidateResult(false, "Product has associated orders.");

                string imageUrl = product.ImageUrl;

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(imageUrl))
                    await _storageService.DeleteImageAsync(imageUrl);

                return new ValidateResult(true, "Product deleted successfully.");

        }
        public async Task<Product> GetProductByIdAsync(int id) =>
         await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == id);
        public async Task<Product> GetProductByIdWithCategoryAsync(int id)
        {
            return await _context.Products
                .Include(p => p.Category)
                .SingleOrDefaultAsync(p => p.ProductId == id);
        }
        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId)
        {
            return await GetQueryableWithIncludes(p => p.Category)
                  .Where(p => p.CategoryId == categoryId).ToListAsync();
        }
        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            return await _context.Products.ToListAsync();
        }
        public async Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedProductsAsync(int pageNumber, int pageSize)
        {
            pageNumber = pageNumber < 1 ? 1 : pageNumber;
            var totalCount = await _context.Products.CountAsync();
            var items = await _context.Products.Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (items, totalCount);
        }
        public async Task<ValidateResult> CreateProductAsync(Product product, IFormFile? imageFile)
        {
            var validationResult = ValidateProduct(product);
            if (!validationResult.isValid)
            {
                return validationResult;
            }

            try
            {
               if(imageFile != null)
                product.ImageUrl = await _storageService.StoreImageAsync(imageFile);

                await _context.Products.AddAsync(product);
                await _context.SaveChangesAsync();
                return new ValidateResult(true, "Product created successfully.");
            }
            catch (Exception ex)
            {
                return new ValidateResult(false, "An error occurred while creating the product: " + ex.Message);
            }
        }
        public async Task<ValidateResult> UpdateProductAsync(Product product, IFormFile? imageFile = null)
        {
            var validationResult = ValidateProduct(product);
            if (!validationResult.isValid) return validationResult;

            string oldImageUrl = null;
            string newImageUrl = null;
            Product existingProduct = null;

            try
            {
                // Get tracked entity using FindAsync
                 existingProduct = await _context.Products
                    .FindAsync(product.ProductId);

                if (existingProduct == null)
                    return new ValidateResult(false, "Product not found.");

                // Preserve old image URL before any changes
                 oldImageUrl = existingProduct.ImageUrl;
                 newImageUrl = null;

                // Update scalar properties
                _context.Entry(existingProduct).CurrentValues.SetValues(product);

                // Handle image update
                if (imageFile != null)
                {
                    newImageUrl = await _storageService.StoreImageAsync(imageFile);
                    existingProduct.ImageUrl = newImageUrl;
                }

                // Update manual properties
                existingProduct.UpdatedDate = QatarDateTime.Now;

                // Update navigation properties if needed
                // _context.Entry(existingProduct).Reference(p => p.Category).CurrentValue = product.Category;

                await _context.SaveChangesAsync();

                // Cleanup old image after successful save
                if (imageFile != null && !string.IsNullOrEmpty(oldImageUrl))
                {
                    await _storageService.DeleteImageAsync(oldImageUrl);
                }

                return new ValidateResult(true, "Product updated successfully.");
            }
            catch (DbUpdateConcurrencyException)
            {
                return new ValidateResult(false, "Concurrency error. Refresh and try again.");
            }
            catch (Exception ex)
            {
                // Cleanup new image if save failed
                if (newImageUrl != null)
                {
                    await _storageService.DeleteImageAsync(newImageUrl);
                }
                return new ValidateResult(false, $"Update failed: {ex.Message}");
            }
        }

        public async Task<IEnumerable<ProductViewModel>> GetActiveProducts()
        {
            return await _context.Products
                .Include(p => p.Category)

                .AsSplitQuery()
                .Where(p => p.IsActive)
                .Select(p => new ProductViewModel
                {
                    ProductId = p.ProductId,
                    Name = p.Name,
                    Price = p.Price,
                    OriginalPrice = p.OriginalPrice,
                    CategoryName = p.Category.Name,
                    ImageUrl = p.ImageUrl,
                    IsActive = p.IsActive,
                    Quantity = p.Quantity,
                    Sizes = p.Sizes.Select(c => c.Trim()).ToList(),
                    Lengths = p.Lengths.Select(c => c.Trim()).ToList()
                })
                .ToListAsync();


        }

        public async Task<List<ProductViewModel>> GetFeaturedProductAsync()
        {
            return await _context.Products
                .Include(p => p.Category)

                .AsSplitQuery()
                .Where(p => p.IsFeatured && p.IsActive)
                .OrderByDescending(p => p.CreatedDate)
                .Select(p => new ProductViewModel
                {
                    ProductId = p.ProductId,
                    Name = p.Name,
                    Price = p.Price,
                    OriginalPrice = p.OriginalPrice,
                    CategoryId = p.CategoryId,
                    ImageUrl = p.ImageUrl,
                    CreatedDate = p.CreatedDate,
                    IsActive = p.IsActive,
                    IsFeatured = p.IsFeatured,
                    Quantity = p.Quantity,
                    Sizes = p.Sizes.Select(c => c.Trim()).ToList(),
                    Lengths = p.Lengths.Select(c => c.Trim()).ToList()
                })
                .ToListAsync();
        }

        public async Task<List<ProductViewModel>> GetRelatedProductsAsync(string? categoryName = null,int currentProductId = 0, int maxItems = 4,bool showExcludedOnly = false)
        {
            return await  _context.Products
                .Include(p => p.Category)
                .Where(p =>p.IsActive && p.IsExcludedFromRelated == showExcludedOnly)
                .Select(p => new ProductViewModel
                {
                    ProductId = p.ProductId,
                    Name = p.Name,
                    Price = p.Price,
                    OriginalPrice = p.OriginalPrice,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category.Name!,
                    ImageUrl = p.ImageUrl,
                    CreatedDate = p.CreatedDate,
                    IsActive = p.IsActive,
                    IsExcludedFromRelated = p.IsExcludedFromRelated,
                    Sizes = p.Sizes.Select(s => s.Trim()).ToList(),
                    Lengths = p.Lengths.Select(l => l.Trim()).ToList(),
                    Quantity = p.Quantity
                })
                .ToListAsync();
        }



        private ValidateResult ValidateProduct(Product product)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(product.Name))
            {
                errors.Add("Name is required.");
            }

            if (product.Price <= 0)
            {
                errors.Add("Price must be greater than 0.");
            }

            if (string.IsNullOrWhiteSpace(product.ImageUrl))
            {
                errors.Add("Image URL is required.");
            }

            if (product.CategoryId <= 0)
            {
                errors.Add("Category is required.");
            }

            if (product.Lengths.Count < 2 || product.Lengths.Count > 5)
            {
                errors.Add("Must have 2-5 lengths.");
            }

            if (product.Sizes.Count < 2 || product.Sizes.Count > 5)
            {
                errors.Add("Must have 2-5 sizes.");
            }

            if (errors.Any())
            {
                return new ValidateResult(false, string.Join(", ", errors));
            }

            return new ValidateResult(true, "Validation succeeded.");
        }

        private IQueryable<Product> GetQueryableWithIncludes(params Expression<Func<Product, object>>[] includes)
        {
            IQueryable<Product> query = _context.Products;

            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            return query;
        }

        public async Task<(IEnumerable<ProductViewModel> Products, int TotalCount)> GetFilteredProductsAsync(string category, decimal minPrice, decimal maxPrice, string sortBy, int page, int pageSize, bool showExcludedOnly = false)
        {
            var query = _context.Products
        .Include(p => p.Category)
        .Where(p => p.IsActive &&(showExcludedOnly ? p.IsExcludedFromRelated :true));

            
            if (!string.IsNullOrEmpty(category) && category.ToLower() != "all")
            {
                query = query.Where(p => p.Category.Name.ToLower() == category.ToLower());
            }


            // Price filter
            query = query.Where(p => p.Price >= minPrice && p.Price <= maxPrice);

            // Sorting
            switch (sortBy)
            {
                case "price-asc":
                    query = query.OrderBy(p => p.Price);
                    break;
                case "price-desc":
                    query = query.OrderByDescending(p => p.Price);
                    break;
                default:
                    query = query.OrderBy(p => p.ProductId);
                    break;
            }

            // Pagination
            var totalCount = await query.CountAsync();
            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductViewModel
                {
                    ProductId = p.ProductId,
                    Name = p.Name,
                    Price = p.Price,
                    OriginalPrice = p.OriginalPrice,
                    CategoryName = p.Category.Name,
                    ImageUrl = p.ImageUrl,
                    Quantity = p.Quantity,
                    Sizes = p.Sizes.Select(c => c.Trim()).ToList(),
                    Lengths = p.Lengths.Select(c => c.Trim()).ToList()
                })
                .AsSplitQuery()
                .ToListAsync();

            return (products, totalCount);
        }

        public async Task<(decimal MinPrice, decimal MaxPrice)> GetPriceRangeAsync(string category = null)
        {
            var query = _context.Products.Where(p => p.IsActive);

            if (!string.IsNullOrEmpty(category) && category.ToLower() != "all")
            {
                query = query.Where(p => p.Category.Name.ToLower() == category.ToLower());
            }

            var result = await query.GroupBy(p => 1).Select(g => new
            {
                MinPrice = g.Min(p => p.Price),
                MaxPrice = g.Max(p => p.Price)
            }).FirstOrDefaultAsync();

            return (result?.MinPrice ?? 0, result?.MaxPrice ?? 0);
        }

        public async Task<IEnumerable<ProductViewModel>> SearchProductsAsync(string searchTerm)
        {
            var normalizedSearch = searchTerm.Trim().ToLower();

            return await _context.Products
                .Include(p => p.Category)
                .Where(p =>
                    p.Name.ToLower().Contains(normalizedSearch) ||
                    p.Description.ToLower().Contains(normalizedSearch)
                )
                .Select(p => new ProductViewModel
                {

                    ProductId = p.ProductId,
                    Name = p.Name,
                    Price = p.Price,
                    ImageUrl = p.ImageUrl,
                    CategoryName = p.Category.Name,
                    // Add other necessary fields
                    Description = p.Description,
                    Lengths = p.Lengths,
                    Sizes = p.Sizes,
                    Quantity = p.Quantity
                })
                .ToListAsync();
        }

    }

}
