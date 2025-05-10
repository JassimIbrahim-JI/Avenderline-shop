using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LavenderLine.Models
{
    public class CarouselImage
    {
        public int Id { get; set; }

        public string? ImageUrl { get; set; }

        [StringLength(200, ErrorMessage = "Caption cannot exceed 200 characters.")]
        public string? Caption { get; set; }


        [StringLength(200, ErrorMessage = "Desciption cannot exceed 200 characters.")]
        public string? Description { get; set; }

        [NotMapped]
        [Required(ErrorMessage = "Image file is required.")]
        public IFormFile ImageFile { get; set; } = default!;

        public bool IsActiveHome { get; set; } = true;
        public bool IsActiveShop { get; set; }
        public bool IsActiveArrivals { get; set; }
    }
}
