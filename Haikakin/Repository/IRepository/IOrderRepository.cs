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

        Order GetOrder(int orderId);

        bool OrderExists(int id);

        int CreateOrder(Order order);

        bool UpdateOrder(Order order);

        bool DeleteOrder(Order order);

        bool Save();
    }
}
