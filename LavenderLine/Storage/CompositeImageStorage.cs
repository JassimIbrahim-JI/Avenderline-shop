namespace LavenderLine.Storage
{
    public class CompositeImageStorage : IImageStorageService
    {
        private readonly IImageStorageService _googleStorage;
        private readonly IImageStorageService _localStorage;

        public CompositeImageStorage(
             IImageStorageService googleStorage,
            IImageStorageService localStorage)
        {
            _googleStorage = googleStorage;
            _localStorage = localStorage;
        }

        public string FolderName => _localStorage.FolderName;
        public async Task<string> StoreImageAsync(IFormFile imageFile)
        {
            string googleUrl = null;
            string localUrl = null;
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
            var customFile = new CustomFormFile(imageFile, fileName);

            try
            {
                googleUrl = await _googleStorage.StoreImageAsync(customFile);
                localUrl = await _localStorage.StoreImageAsync(customFile);
                return googleUrl;
            }
            catch (Exception)
            {
                if (googleUrl != null) await _googleStorage.DeleteImageAsync(googleUrl);
                if (localUrl != null) await _localStorage.DeleteImageAsync(localUrl);
                throw;
            }
        }

        public async Task DeleteImageAsync(string imageUrl)
        {

            if (string.IsNullOrEmpty(imageUrl)) return;
            // Delete from both google and Local
            await _googleStorage.DeleteImageAsync(imageUrl);

            // Extract filename from google URL and delete Local file
            var fileName = new Uri(imageUrl).Segments.Last();
            var localUrl = $"/{_localStorage.FolderName}/{fileName}";
            await _localStorage.DeleteImageAsync(localUrl);
        }
    }
}
