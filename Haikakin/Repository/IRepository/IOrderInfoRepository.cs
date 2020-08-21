using Haikakin.Models;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Haikakin.Models.OrderModel;

namespace Haikakin.Repository.IRepository
{
    public interface IOrderInfoRepository
    {
        ICollection<OrderInfo> GetOrderInfosByOrderId(int orderInfoId);

        bool OrderInfoExists(int id);

        bool CreateOrderInfo(OrderInfo orderInfo);

        bool UpdateOrderInfo(OrderInfo orderInfo);

        bool Save();
    }
}
