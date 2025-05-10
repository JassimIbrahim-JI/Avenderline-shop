namespace LavenderLine.Extensions
{
    public static class FileUploadExtenstions
    {
        public static readonly string[] SupportedExtensions = { ".jpg", ".jpeg", ".png" };

        public static bool IsValidImage(this IFormFile file, out string errorMessage)
        {
            errorMessage = null;

            if (file == null || file.Length == 0)
            {
                errorMessage = "No File Uploaded.";
                return false;
            }

            // Check file extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (Array.IndexOf(SupportedExtensions, extension) == -1)
            {
                errorMessage = "Invalid file type. Allowed Types: .jpg, .jpeg, .png";
                return false;
            }

            // Check file size (limit to 5 MB)
            if (file.Length > 5 * 1024 * 1024)
            {
                errorMessage = "File size exceeds the limit of 5 MB.";
                return false;
            }

            return true;
        }
    }
}
