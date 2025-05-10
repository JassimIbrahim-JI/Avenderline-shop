namespace LavenderLine.Storage
{
    public class LocalImageStorage : IImageStorageService
    {
        private readonly IWebHostEnvironment _env;
        private readonly string _folderName;

        public LocalImageStorage(IWebHostEnvironment env, string folderName)
        {
            _env = env;
            _folderName = folderName;
        }

        public string FolderName => _folderName;

        public Task DeleteImageAsync(string imageUrl)
        {
            var filePath = Path.Combine(_env.WebRootPath, imageUrl.TrimStart('/'));
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            return Task.CompletedTask;
        }

        public async Task<string> StoreImageAsync(IFormFile imageFile)
        {
            if (imageFile.Length > 8 * 1024 * 1024)
                throw new ArgumentException("File size exceeds 8MB.");

            var uploadPath = Path.Combine(_env.WebRootPath, _folderName);
            Directory.CreateDirectory(uploadPath);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
            var filePath = Path.Combine(uploadPath, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await imageFile.CopyToAsync(stream);

            return $"/{_folderName}/{fileName}";
        }
    }
}
