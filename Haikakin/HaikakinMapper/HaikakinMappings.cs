using AutoMapper;
using Haikakin.Models;
using Haikakin.Models.AnnouncementModel;
using Haikakin.Models.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Haikakin.Models.OrderModel;

namespace Haikakin.HaikakinMapper
{
    public class HaikakinMappings : Profile
    {

        public HaikakinMappings()
        {
            CreateMap<Order, OrderDto>().ReverseMap();
            CreateMap<Order, OrderCreateDto>().ReverseMap();
            CreateMap<Product, ProductUpsertDto>().ReverseMap();
            CreateMap<Announcement, AnnouncementCreateDto>().ReverseMap();
        }
    }
}