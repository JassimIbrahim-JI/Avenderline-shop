using LavenderLine.Models;
using LavenderLine.Storage;
using LavenderLine.Validate;
using Microsoft.EntityFrameworkCore;

namespace LavenderLine.Services
{
    public interface ICarouselImageService
    {
        Task<IEnumerable<CarouselImage>> GetAllImagesAsync();
        Task<CarouselImage?> GetImageByIdAsync(int id);
        Task<ValidateResult> AddImageAsync(CarouselImage image);
        Task<ValidateResult> UpdateImageAsync(CarouselImage image);
        Task<ValidateResult> DeleteImageAsync(int id);
    }

    public class CarouselImageService : ICarouselImageService
    {
        private readonly EcommerceContext _commerceContext;
        private readonly IImageStorageService _storageService;

        public CarouselImageService(EcommerceContext commerceContext, IImageStorageFactory storageFactory)
        {
            _commerceContext = commerceContext;
            _storageService = storageFactory.GetStorageService("Carousel");
        }

        public async Task<IEnumerable<CarouselImage>> GetAllImagesAsync()
            => await _commerceContext.Carousels.ToListAsync();

        public async Task<CarouselImage?> GetImageByIdAsync(int id)
            => await _commerceContext.Carousels.FirstOrDefaultAsync(c => c.Id == id);

        public async Task<ValidateResult> AddImageAsync(CarouselImage image)
        {
            if (image.ImageFile == null || image.ImageFile.Length == 0)
                return new ValidateResult(false, "Image file is required.");

            var executionStrategy = _commerceContext.Database.CreateExecutionStrategy();

            return await executionStrategy.ExecuteAsync(async () =>
            {
                using var transaction = await _commerceContext.Database.BeginTransactionAsync();
                try
                {
                    image.ImageUrl = await _storageService.StoreImageAsync(image.ImageFile);
                    _commerceContext.Carousels.Add(image);
                    await _commerceContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return new ValidateResult(true, "Image added successfully.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    if (!string.IsNullOrEmpty(image.ImageUrl))
                        await _storageService.DeleteImageAsync(image.ImageUrl);

                    return new ValidateResult(false, $"Failed to add image: {ex.Message}");
                }
            });
        }

        public async Task<ValidateResult> UpdateImageAsync(CarouselImage image)
        {
            var existingImage = await _commerceContext.Carousels.FindAsync(image.Id);
            if (existingImage == null)
                return new ValidateResult(false, "Image not found.");

            string oldImageUrl = existingImage.ImageUrl;
            string newImageUrl = null;

            var executionStrategy = _commerceContext.Database.CreateExecutionStrategy();

            return await executionStrategy.ExecuteAsync(async () =>
            {
                using var transaction = await _commerceContext.Database.BeginTransactionAsync();
                try
                {
                    if (image.ImageFile != null)
                    {
                        newImageUrl = await _storageService.StoreImageAsync(image.ImageFile);
                        existingImage.ImageUrl = newImageUrl;
                    }

                    existingImage.Caption = image.Caption;
                    existingImage.Description = image.Description;

                    _commerceContext.Carousels.Update(existingImage);
                    await _commerceContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    if (image.ImageFile != null && !string.IsNullOrEmpty(oldImageUrl))
                        await _storageService.DeleteImageAsync(oldImageUrl);

                    return new ValidateResult(true, "Image updated successfully.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    if (newImageUrl != null)
                        await _storageService.DeleteImageAsync(newImageUrl);

                    return new ValidateResult(false, $"Failed to update image: {ex.Message}");
                }
            });
        }

        public async Task<ValidateResult> DeleteImageAsync(int id)
        {
            var carousel = await _commerceContext.Carousels.FindAsync(id);
            if (carousel == null)
                return new ValidateResult(false, "Image not found.");

            string imageUrl = carousel.ImageUrl;

            var executionStrategy = _commerceContext.Database.CreateExecutionStrategy();

            return await executionStrategy.ExecuteAsync(async () =>
            {
                using var transaction = await _commerceContext.Database.BeginTransactionAsync();
                try
                {
                    _commerceContext.Carousels.Remove(carousel);
                    await _commerceContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    if (!string.IsNullOrEmpty(imageUrl))
                        await _storageService.DeleteImageAsync(imageUrl);

                    return new ValidateResult(true, "Image deleted successfully.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return new ValidateResult(false, $"Failed to delete image: {ex.Message}");
                }
            });
        }
    }
}
