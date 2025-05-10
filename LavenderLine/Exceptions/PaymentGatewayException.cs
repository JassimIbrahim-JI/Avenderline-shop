namespace LavenderLine.Exceptions
{
    public class PaymentGatewayException : Exception
    {
        public PaymentGatewayException() { }
        public PaymentGatewayException(string message) : base(message) { }
        public PaymentGatewayException(string message, Exception inner)
            : base(message, inner) { }
    }
}
