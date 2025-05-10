
namespace LavenderLine.DTO
{
    public record WishlistItemDto(
    int ProductId,
    string Name,
    decimal Price,
    decimal? OriginalPrice,
    string ImageUrl,
    int Quantity,
    string Category,
    bool IsFavorite
);
}
