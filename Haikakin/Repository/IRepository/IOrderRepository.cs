using System;
using System.Collections.Generic;
using Haikakin.Models.OrderModel;

namespace Haikakin.Repository.IRepository
{
    public interface IOrderRepository
    {
        ICollection<Order> GetOrdersInUser(int userId);

        ICollection<Order> GetOrdersWithTimeRange(DateTime startTime, DateTime endTime, short? orderStatus);

        Order GetOrder(int orderId);

        Order GetOrdersInPaySerial(string paySerial);

        bool OrderExists(int id);

        Order CreateOrder(Order order);

        bool UpdateOrder(Order order);

        bool Save();
    }
}
