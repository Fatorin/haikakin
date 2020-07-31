using Haikakin.Data;
using Haikakin.Models;
using Haikakin.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Repository
{
    public class OrderRepository : IOrderRepository
    {
        private readonly ApplicationDbContext _db;

        public OrderRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public Order CreateOrder(Order order)
        {
            var db = _db.Orders.Add(order);
            _db.SaveChanges();
            return db.Entity;
        }

        public bool DeleteOrder(Order order)
        {
            _db.Orders.Remove(order);
            return Save();
        }

        public Order GetOrder(int orderId)
        {
            var order = _db.Orders.FirstOrDefault(o => o.OrderId == orderId);
            var orderInfos = _db.OrderInfos.Where(o => o.OrderId == orderId).ToList();
            if (order == null) return order;
            order.OrderInfos = orderInfos;
            return order;
        }

        public ICollection<Order> GetOrders()
        {
            return _db.Orders.OrderBy(u => u.OrderId).ToList();
        }

        public ICollection<Order> GetOrdersInUser(int userId)
        {
            return _db.Orders.Where(u => u.UserId == userId).ToList();
        }

        public bool OrderExists(int id)
        {
            bool value = _db.Orders.Any(u => u.OrderId == id);
            return value;
        }

        public bool Save()
        {
            return _db.SaveChanges() >= 0 ? true : false;
        }

        public bool UpdateOrder(Order order)
        {
            _db.Orders.Update(order);
            return Save();
        }
    }
}
