using AutoMapper;
using LavenderLine.ViewModels.Carts;

namespace LavenderLine.DTO
{
    public class CartProfile : Profile
    {
        public CartProfile()
        {
            CreateMap<CartDto, CartViewModel>();

            CreateMap<CartItemDto, CartItemViewModel>()
                .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity));
        }
    }
}
