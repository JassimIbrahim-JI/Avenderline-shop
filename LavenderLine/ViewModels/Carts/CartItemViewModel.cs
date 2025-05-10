namespace LavenderLine.ViewModels.Carts
{
    public class CartItemViewModel
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string Length { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string? SpecialRequest { get; set; } = string.Empty;
        public int MaxStock { get; set; }
        public int AvailableStock { get; set; }
    }
}
