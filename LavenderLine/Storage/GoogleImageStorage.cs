using Google.Cloud.Storage.V1;

namespace LavenderLine.Storage
{
      public class GoogleImageStorage : IImageStorageService
     {
         private readonly StorageClient _storageClient;
         private readonly string _bucketName;

         public GoogleImageStorage(StorageClient storageClient, string bucketName)
         {
             _storageClient = storageClient;
             _bucketName = bucketName;
         }

         public string FolderName => _bucketName;
         public async Task<string> StoreImageAsync(IFormFile imageFile)
         {
             if (imageFile.Length > 5 * 1024 * 1024)
                 throw new ArgumentException("File size exceeds 5MB.");


             var fileName = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";

            using var stream = new MemoryStream();
            await imageFile.CopyToAsync(stream);
            stream.Position = 0;

            await _storageClient.UploadObjectAsync(
                _bucketName,
                fileName,
                imageFile.ContentType,
                stream
            );

            return $"https://storage.googleapis.com/{_bucketName}/{fileName}";
        }

         public async Task DeleteImageAsync(string imageUrl)
         {
            var uri = new Uri(imageUrl);
            var fileName = uri.Segments.Last();
            await _storageClient.DeleteObjectAsync(_bucketName, fileName);
        }
     }
}
