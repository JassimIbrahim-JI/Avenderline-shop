using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LavenderLine.ViewModels.Banners
{
    public class EditCarouselViewModel
    {
        public int Id { get; set; }

        [Required]
        public string ImageUrl { get; set; }

        [StringLength(200, ErrorMessage = "Caption cannot exceed 200 characters")]
        public string? Caption { get; set; }

        [StringLength(200,ErrorMessage = "Description cannot exceed 200 characters")]
        public string? Description { get; set; }

        [NotMapped]
        [Display(Name = "New Image (Optional)")]
        public IFormFile? ImageFile { get; set; }
    }
}
