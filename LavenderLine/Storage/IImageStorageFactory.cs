namespace LavenderLine.Storage
{
    public interface IImageStorageFactory
    {
        IImageStorageService GetStorageService(string target);
    }
}
