namespace LavenderLine.ViewModels.Wishlists
{
    // ViewModels/WishlistViewModels.cs
    public class WishlistViewModel
    {
        public List<WishlistItemViewModel> Items { get; set; } = new();
        public int Count { get; set; }
    }
}
