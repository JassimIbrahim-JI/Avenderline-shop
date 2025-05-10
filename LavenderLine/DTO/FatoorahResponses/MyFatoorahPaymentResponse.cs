namespace LavenderLine.DTO.FatoorahResponses
{
    public class MyFatoorahPaymentResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public PaymentData Data { get; set; }
        public class PaymentData
        {
            public string PaymentURL { get; set; }
            public string PaymentId { get; set; }
        }
    }
}
