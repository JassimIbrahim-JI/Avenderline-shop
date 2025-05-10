namespace LavenderLine.Storage
{
    public class CustomFormFile : IFormFile
    {
        private readonly IFormFile _file;
        private readonly string _fileName;

        public CustomFormFile(IFormFile file, string fileName)
        {
            _file = file;
            _fileName = fileName;
        }

        // Override the FileName property
        public string FileName => _fileName;

        // Delegate other properties and methods to the original IFormFile
        public string ContentType => _file.ContentType;
        public long Length => _file.Length;
        public string Name => _file.Name;
        public string ContentDisposition => _file.ContentDisposition;
        public IHeaderDictionary Headers => _file.Headers;

        // Implement required methods
        public Stream OpenReadStream() => _file.OpenReadStream();

        public void CopyTo(Stream target) => _file.CopyTo(target);

        public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default) =>
            _file.CopyToAsync(target, cancellationToken);
    }
}
