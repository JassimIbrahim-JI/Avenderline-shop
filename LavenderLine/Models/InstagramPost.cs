using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LavenderLine.Models
{
    public class InstagramPost
    {
        public int Id { get; set; }
        public string? ImageUrl { get; set; }

        [Required]
        [MaxLength(500)]
        public string Caption { get; set; } = string.Empty;

        [MaxLength(100)]
        [RegularExpression(@"(^#\w+)(\s#\w+)*$", ErrorMessage = "Hashtags must start with # and be space-separated.")]
        public string? Hashtag { get; set; }

        [Url(ErrorMessage = "Please enter a valid Instagram post URL.")]
        public string? PostUrl { get; set; }

        [NotMapped]
        [Required(ErrorMessage = "Image file is required.")]
        public IFormFile ImageFile { get; set; } = default!;
    }
}
