namespace LavenderLine.DTO
{
    public record CartDto(
     List<CartItemDto> Items,
     decimal Total,
     int Count
 );
}
