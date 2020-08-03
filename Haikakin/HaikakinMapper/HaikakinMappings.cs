using AutoMapper;
using Haikakin.Models;
using Haikakin.Models.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.HaikakinMapper
{
    public class HaikakinMappings : Profile
    {

        public HaikakinMappings()
        {
            CreateMap<User, UserUpdateDto>().ReverseMap();
            CreateMap<Order, OrderDto>().ReverseMap();
            CreateMap<Order, OrderCreateDto>().ReverseMap();
            CreateMap<Product, ProductDto>().ReverseMap();
            CreateMap<Product, ProductUpsertDto>().ReverseMap();
        }
    }
}
