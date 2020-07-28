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
    public class OrderInfoRepository : IOrderInfoRepository
    {
        private readonly ApplicationDbContext _db;

        public OrderInfoRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public bool CreateOrderInfo(OrderInfo orderInfo)
        {
            //減少庫存並新增訂單詳細資訊
            var productId = orderInfo.ProductId;
            var product = _db.Products.FirstOrDefault(u => u.ProductId == productId);
            product.Stock -= orderInfo.Count;
            //取得OrderInfoID用
            var temp = _db.OrderInfos.Add(orderInfo);
            _db.Products.Update(product);
            _db.SaveChanges();
            //取得OrderInfoID用
            var orderInfoId = temp.Entity.OrderInfoId;
            //抓指定可用的數量訂購
            var productInfos = _db.ProductInfos.Where(p => p.ProductId == orderInfo.ProductId).Where(p => p.ProductStatus == ProductInfo.ProductStatusEnum.NotUse).ToList().Take(orderInfo.Count);
            foreach (ProductInfo productInfo in productInfos)
            {
                productInfo.ProductStatus = ProductInfo.ProductStatusEnum.Lock;
                productInfo.LastUpdateTime = DateTime.UtcNow;
                productInfo.OrderInfoId = orderInfoId;
                productInfo.ProductId = productId;
                _db.ProductInfos.Update(productInfo);
            }
            return Save();
        }

        public bool UpdateOrderInfo(OrderInfo orderInfo)
        {
            _db.OrderInfos.Update(orderInfo);
            return Save();
        }

        public ICollection<OrderInfo> GetOrderInfosByOrderId(int orderId)
        {
            return _db.OrderInfos.Include(o => o.OrderId).Where(o => o.OrderId == orderId).ToList();
        }

        public bool OrderInfoExists(int id)
        {
            bool value = _db.OrderInfos.Any(u => u.OrderInfoId == id);
            return value;
        }

        public bool Save()
        {
            return _db.SaveChanges() >= 0 ? true : false;
        }
    }
}
