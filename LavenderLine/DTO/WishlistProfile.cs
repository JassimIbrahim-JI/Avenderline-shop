using AutoMapper;
using LavenderLine.ViewModels.Wishlists;

namespace LavenderLine.DTO
{
    public class WishlistProfile : Profile
    {
        public WishlistProfile()
        {
            CreateMap<WishlistDto, WishlistViewModel>();
            CreateMap<WishlistItemDto, WishlistItemViewModel>();
        }
    }
}
