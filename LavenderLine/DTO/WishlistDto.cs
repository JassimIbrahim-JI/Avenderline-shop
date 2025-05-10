namespace LavenderLine.DTO
{
    public record WishlistDto(
      List<WishlistItemDto> Items,
      int Count
  );
}
