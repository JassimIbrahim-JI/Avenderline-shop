namespace LavenderLine.DTO.FatoorahResponses
{
    public class MyFatoorahPaymentStatus
    {
        public long OrderId { get; set; }
        public bool IsSuccess { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
    }
}
