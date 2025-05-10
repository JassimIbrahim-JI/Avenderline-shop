using Google.Cloud.Storage.V1;
using LavenderLine.Settings;

namespace LavenderLine.Storage
{
    public class ImageStorageFactory : IImageStorageFactory
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
         private readonly StorageClient _storageClient;

        public ImageStorageFactory(IConfiguration config, IWebHostEnvironment env, StorageClient storageClient)
        {
            _config = config;
            _env = env;
            _storageClient = storageClient;
        }

        public IImageStorageService GetStorageService(string target)
        {
            var settings = _config.GetSection($"Storage:{target}")
                           .Get<StorageSettings>();

            return settings.Mode switch
            {
                "Google" => new GoogleImageStorage(_storageClient, settings.GoogleBucket),
                "Local" => new LocalImageStorage(_env, settings.LocalFolder),
                "Composite" => new CompositeImageStorage(
                    new GoogleImageStorage(_storageClient, settings.GoogleBucket),
                    new LocalImageStorage(_env, settings.LocalFolder)
                ),
                _ => throw new InvalidOperationException($"Invalid mode: {settings.Mode}")
            };
        }
    }
}
