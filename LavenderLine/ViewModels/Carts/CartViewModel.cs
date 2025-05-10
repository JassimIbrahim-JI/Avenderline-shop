namespace LavenderLine.ViewModels.Carts
{
    // ViewModels/CartViewModels.cs
    public class CartViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new();
        public decimal Total { get; set; }
        public int Count { get; set; }
    }
}
