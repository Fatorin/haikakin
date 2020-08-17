using Haikakin.Models;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Repository.IRepository
{
    public interface IOrderRepository
    {
        ICollection<Order> GetOrders();

        ICollection<Order> GetOrdersInUser(int userId);

        ICollection<Order> GetOrdersWithTimeRange(DateTime startTime,DateTime endTime);

        Order GetOrder(int orderId);

        Order GetOrdersInPaySerial(string paySerial);

        bool OrderExists(int id);

        Order CreateOrder(Order order);

        bool UpdateOrder(Order order);

        bool Save();
    }
}
