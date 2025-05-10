namespace LavenderLine.ViewModels.Orders
{
    public class OrderListViewModel
    {
        public IEnumerable<Order> Orders { get; set; }
        public PaginationModel Pagination { get; set; }
        public string CurrentSort { get; set; }
        public string CurrentDirection { get; set; }
        public FilterParams FilterParams { get; set; }
    }
}
