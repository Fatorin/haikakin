using Haikakin.Data;
using Haikakin.Models;
using Haikakin.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Haikakin.Models.OrderModel;

namespace Haikakin.Repository
{
    public class ProductInfoRepository : IProductInfoRepository
    {
        private readonly ApplicationDbContext _db;

        public ProductInfoRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public bool CreateProductInfo(ProductInfo productInfo)
        {
            var product = _db.Products.SingleOrDefault(p => p.ProductId == productInfo.ProductId);

            if (product == null) return false;

            _db.ProductInfos.Add(productInfo);
            product.Stock += 1;

            _db.Products.Update(product);

            return Save();
        }

        public bool UpdateProductInfo(ProductInfo productInfo)
        {
            if (productInfo == null) return false;

            _db.ProductInfos.Update(productInfo);

            //更新庫存
            var product = _db.Products.SingleOrDefault(p => p.ProductId == productInfo.ProductId);
            
            int count = _db.ProductInfos.Where(
                p => p.ProductId == product.ProductId &&
                p.ProductStatus == ProductInfo.ProductStatusEnum.NotUse)
                .Count();

            product.Stock = count;

            _db.Products.Update(product);

            return Save();
        }

        public ProductInfo GetProductInfo(int productInfoId)
        {
            return _db.ProductInfos.SingleOrDefault(p => p.ProductInfoId == productInfoId);
        }

        public bool DeleteProductInfo(int productInfoId)
        {
            var obj = _db.ProductInfos.SingleOrDefault(p => p.ProductInfoId == productInfoId);
            _db.ProductInfos.Remove(obj);
            return Save();
        }

        public ICollection<ProductInfo> GetProductInfos()
        {
            return _db.ProductInfos.OrderBy(p => p.ProductInfoId).ToList();
        }
        public ICollection<ProductInfo> GetProductInfosByOrderInfoId(int orderInfoId)
        {
            return _db.ProductInfos.Where(p => p.OrderInfoId == orderInfoId).ToList();
        }

        public bool ProductInfoExists(int id)
        {
            bool value = _db.ProductInfos.Any(u => u.ProductInfoId == id);
            return value;
        }

        public bool ProductInfoSerialExists(int productId, string serial)
        {
            bool value = _db.ProductInfos.Any(u => u.Serial == serial && u.ProductId == productId);
            return value;
        }

        public bool Save()
        {
            return _db.SaveChanges() >= 0 ? true : false;
        }
    }
}
