namespace LavenderLine.ViewModels.Carts
{
    public class ColorSizeModalViewModel
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty;

        public decimal ProductPrice { get; set; }

        public IList<string> AvailableLength { get; set; } = new List<string>();

        public IList<string> AvailableSizes { get; set; } = new List<string>();

        public int MaxQuantity { get; set; }
    }
}
