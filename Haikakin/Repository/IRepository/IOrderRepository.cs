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

        Order GetOrder(int OrderId);

        bool OrderExists(int id);

        bool CreateOrder(Order Order);

        bool UpdateOrder(Order Order);

        bool DeleteOrder(Order Order);

        bool Save();
    }
}
