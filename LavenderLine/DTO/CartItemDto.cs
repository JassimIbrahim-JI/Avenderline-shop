namespace LavenderLine.DTO
{
    public record CartItemDto(
        int ProductId,
        string Name,
        string ImageUrl,
        decimal Price,
        int Quantity,
        string Length,
        string Size,
        string SpecialRequest,
        int MaxStock
    );
}
