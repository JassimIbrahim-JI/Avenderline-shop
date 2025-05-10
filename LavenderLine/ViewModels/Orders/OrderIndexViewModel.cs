using LavenderLine.Enums.Order;

namespace LavenderLine.ViewModels.Orders
{
    public class OrderIndexViewModel
    {
        public List<Order> Orders { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public OrderStatus? SelectedStatus { get; set; }
        public string SearchQuery { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
