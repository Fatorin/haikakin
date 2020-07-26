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

        public int CreateOrder(Order order)
        {
            var db =_db.Orders.Add(order);
            _db.SaveChanges();
            return db.Entity.Id;
        }

        public bool DeleteOrder(Order order)
        {
            _db.Orders.Remove(order);
            return Save();
        }

        public Order GetOrder(int OrderId)
        {
            return _db.Orders.FirstOrDefault(o => o.Id == OrderId);
        }

        public ICollection<Order> GetOrders()
        {
            return _db.Orders.OrderBy(u => u.Id).ToList();
        }

        public ICollection<Order> GetOrdersInUser(int userId)
        {
            return _db.Orders.Include(o => o.UserId).Where(u => u.UserId == userId).ToList();
        }

        public bool OrderExists(int id)
        {
            bool value = _db.Orders.Any(u => u.Id == id);
            return value;
        }

        public bool Save()
        {
            return _db.SaveChanges() >= 0 ? true : false;
        }

        public bool UpdateOrder(Order Order)
        {
            _db.Orders.Update(Order);
            return Save();
        }
    }
}
