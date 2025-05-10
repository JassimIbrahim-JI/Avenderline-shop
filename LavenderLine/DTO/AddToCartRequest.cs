

namespace LavenderLine.DTO
{
    public record AddToCartRequest(
       int ProductId,
       int Quantity,
       string Length,
       string Size,
       string SpecialRequest
   )
    {
        public AddToCartRequest() : this(0, 0, "", "","") { }
    };
}
