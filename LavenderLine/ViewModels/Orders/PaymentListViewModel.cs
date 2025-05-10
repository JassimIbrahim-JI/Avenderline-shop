namespace LavenderLine.ViewModels.Orders
{
    public class PaymentListViewModel
    {
        public IEnumerable<Payment> Payments { get; set; }
        public PaginationModel Pagination { get; set; }
        public string CurrentSort { get; set; }
        public string CurrentDirection { get; set; }
        public PaymentFilterParams Filters { get; set; }
    }
}
