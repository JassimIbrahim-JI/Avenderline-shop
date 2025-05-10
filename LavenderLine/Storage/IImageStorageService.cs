namespace LavenderLine.Storage
{
    public interface IImageStorageService
    {
        Task<string> StoreImageAsync(IFormFile imageFile);
        Task DeleteImageAsync(string imageUrl);
        string FolderName { get; }
    }
}
